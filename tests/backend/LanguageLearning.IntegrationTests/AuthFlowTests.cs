using System.Net;
using LanguageLearning.Application.Users;
using LanguageLearning.IntegrationTests.Infrastructure;

namespace LanguageLearning.IntegrationTests;

public sealed class AuthFlowTests(ApiFixture fixture) : IClassFixture<ApiFixture>
{
    private ApiFactory Factory => fixture.Factory;

    [Fact]
    public async Task Register_confirm_login_profile_logout_flow()
    {
        var email = TestUsers.NewEmail();
        using var client = Factory.CreateTestClient();

        var register = await client.PostAsync(
            "/api/auth/register",
            new { email, password = TestUsers.Password, displayName = "小明" });
        await TestClient.EnsureStatusAsync(register, HttpStatusCode.Accepted);

        var query = await Factory.Emails.WaitForLinkQueryAsync(email, "/confirm-email");
        var confirm = await client.PostAsync(
            "/api/auth/confirm-email",
            new { userId = query["userId"], token = query["token"] });
        await TestClient.EnsureStatusAsync(confirm, HttpStatusCode.OK);

        var login = await client.LoginAsync(email, TestUsers.Password);
        await TestClient.EnsureStatusAsync(login, HttpStatusCode.OK);

        var me = await client.GetJsonAsync<CurrentUserDto>("/api/users/me");
        Assert.Equal(email, me.Email);
        Assert.Equal("小明", me.DisplayName);
        Assert.Contains("Student", me.Roles);
        Assert.True(me.EmailConfirmed);
        Assert.False(me.OnboardingCompleted);
        Assert.Equal("Asia/Taipei", me.TimeZoneId);

        var logout = await client.LogoutAsync();
        await TestClient.EnsureStatusAsync(logout, HttpStatusCode.NoContent);

        var afterLogout = await client.GetAsync("/api/users/me");
        Assert.Equal(HttpStatusCode.Unauthorized, afterLogout.StatusCode);
    }

    [Fact]
    public async Task Unauthenticated_request_returns_problem_details_not_redirect()
    {
        using var client = Factory.CreateTestClient();

        var response = await client.GetAsync("/api/users/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal("not_authenticated", await TestClient.ReadProblemCodeAsync(response));
    }

    [Fact]
    public async Task Login_requires_confirmed_email_only_after_correct_password()
    {
        var email = TestUsers.NewEmail();
        using var client = Factory.CreateTestClient();
        await TestClient.EnsureStatusAsync(
            await client.PostAsync("/api/auth/register", new { email, password = TestUsers.Password, displayName = "未驗證" }),
            HttpStatusCode.Accepted);

        var wrongPassword = await client.LoginAsync(email, "WrongPass123");
        Assert.Equal(HttpStatusCode.Unauthorized, wrongPassword.StatusCode);
        Assert.Equal("invalid_credentials", await TestClient.ReadProblemCodeAsync(wrongPassword));

        var correctPassword = await client.LoginAsync(email, TestUsers.Password);
        Assert.Equal(HttpStatusCode.Forbidden, correctPassword.StatusCode);
        Assert.Equal("email_not_confirmed", await TestClient.ReadProblemCodeAsync(correctPassword));
    }

    [Fact]
    public async Task Unknown_account_and_wrong_password_return_same_error()
    {
        var email = await TestUsers.RegisterConfirmedAsync(Factory);
        using var client = Factory.CreateTestClient();

        var unknown = await client.LoginAsync(TestUsers.NewEmail(), TestUsers.Password);
        var wrong = await client.LoginAsync(email, "WrongPass123");

        Assert.Equal(HttpStatusCode.Unauthorized, unknown.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, wrong.StatusCode);
        Assert.Equal(await TestClient.ReadProblemCodeAsync(unknown), await TestClient.ReadProblemCodeAsync(wrong));
    }

    [Fact]
    public async Task Account_is_locked_after_repeated_failures()
    {
        var email = await TestUsers.RegisterConfirmedAsync(Factory);
        using var client = Factory.CreateTestClient();

        for (var i = 0; i < 5; i++)
        {
            await client.LoginAsync(email, "WrongPass123");
        }

        var locked = await client.LoginAsync(email, TestUsers.Password);

        Assert.Equal(HttpStatusCode.Locked, locked.StatusCode);
        Assert.Equal("account_locked", await TestClient.ReadProblemCodeAsync(locked));
    }

    [Fact]
    public async Task Register_with_existing_email_returns_same_response_and_sends_notice()
    {
        var email = await TestUsers.RegisterConfirmedAsync(Factory);
        var before = Factory.Emails.For(email).Count;
        using var client = Factory.CreateTestClient();

        var again = await client.PostAsync(
            "/api/auth/register",
            new { email, password = "Another1Pass", displayName = "重複" });

        Assert.Equal(HttpStatusCode.Accepted, again.StatusCode);
        var notices = await Factory.Emails.WaitForCountAsync(email, before + 1);
        Assert.Equal(before + 1, notices.Count);
        Assert.Contains("已註冊", notices[^1].Subject, StringComparison.Ordinal);

        // 原密碼仍可登入，代表沒有建立新帳號或覆寫密碼。
        var login = await client.LoginAsync(email, TestUsers.Password);
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
    }

    [Fact]
    public async Task Forgot_and_reset_password_flow()
    {
        var email = await TestUsers.RegisterConfirmedAsync(Factory);
        using var client = Factory.CreateTestClient();
        const string newPassword = "Brand1NewPass";

        var forgot = await client.PostAsync("/api/auth/forgot-password", new { email });
        await TestClient.EnsureStatusAsync(forgot, HttpStatusCode.Accepted);

        var query = await Factory.Emails.WaitForLinkQueryAsync(email, "/reset-password");
        var reset = await client.PostAsync(
            "/api/auth/reset-password",
            new { userId = query["userId"], token = query["token"], newPassword });
        await TestClient.EnsureStatusAsync(reset, HttpStatusCode.OK);

        var oldLogin = await client.LoginAsync(email, TestUsers.Password);
        Assert.Equal(HttpStatusCode.Unauthorized, oldLogin.StatusCode);

        var newLogin = await client.LoginAsync(email, newPassword);
        Assert.Equal(HttpStatusCode.OK, newLogin.StatusCode);

        // 同一個連結不可重複使用（Security Stamp 已變更）。
        var reuse = await client.PostAsync(
            "/api/auth/reset-password",
            new { userId = query["userId"], token = query["token"], newPassword = "Another1Pass" });
        Assert.Equal(HttpStatusCode.BadRequest, reuse.StatusCode);
        Assert.Equal("invalid_reset_link", await TestClient.ReadProblemCodeAsync(reuse));
    }

    [Fact]
    public async Task Forgot_password_for_unknown_email_returns_same_response_without_email()
    {
        using var client = Factory.CreateTestClient();
        var email = TestUsers.NewEmail();

        var response = await client.PostAsync("/api/auth/forgot-password", new { email });

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        await Factory.Emails.AssertNoneForAsync(email, TimeSpan.FromSeconds(2));
    }

    [Fact]
    public async Task Changing_password_invalidates_other_sessions()
    {
        var email = await TestUsers.RegisterConfirmedAsync(Factory);
        using var deviceA = Factory.CreateTestClient();
        using var deviceB = Factory.CreateTestClient();
        await TestClient.EnsureStatusAsync(await deviceA.LoginAsync(email, TestUsers.Password), HttpStatusCode.OK);
        await TestClient.EnsureStatusAsync(await deviceB.LoginAsync(email, TestUsers.Password), HttpStatusCode.OK);

        var change = await deviceA.PostAsync(
            "/api/auth/change-password",
            new { currentPassword = TestUsers.Password, newPassword = "Changed1Pass" });
        await TestClient.EnsureStatusAsync(change, HttpStatusCode.OK);

        Assert.Equal(HttpStatusCode.OK, (await deviceA.GetAsync("/api/users/me")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await deviceB.GetAsync("/api/users/me")).StatusCode);
    }

    [Fact]
    public async Task Change_password_rejects_wrong_current_password()
    {
        var (client, _) = await TestUsers.LoggedInAsync(Factory);
        using (client)
        {
            var response = await client.PostAsync(
                "/api/auth/change-password",
                new { currentPassword = "WrongPass123", newPassword = "Changed1Pass" });

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Equal("invalid_current_password", await TestClient.ReadProblemCodeAsync(response));
        }
    }

    [Fact]
    public async Task Invalid_confirmation_token_is_rejected()
    {
        var email = TestUsers.NewEmail();
        using var client = Factory.CreateTestClient();
        await client.PostAsync("/api/auth/register", new { email, password = TestUsers.Password, displayName = "x" });
        var query = await Factory.Emails.WaitForLinkQueryAsync(email, "/confirm-email");

        var response = await client.PostAsync(
            "/api/auth/confirm-email",
            new { userId = query["userId"], token = "bm90LWEtdmFsaWQtdG9rZW4" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("invalid_confirmation_link", await TestClient.ReadProblemCodeAsync(response));
    }

    [Fact]
    public async Task Register_validation_errors_use_camel_case_fields()
    {
        using var client = Factory.CreateTestClient();

        var response = await client.PostAsync(
            "/api/auth/register",
            new { email = "not-an-email", password = "weak", displayName = "" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var json = await TestClient.ReadJsonAsync(response);
        var errors = json.GetProperty("errors");
        Assert.True(errors.TryGetProperty("email", out _));
        Assert.True(errors.TryGetProperty("password", out _));
        Assert.True(errors.TryGetProperty("displayName", out _));
        Assert.Equal("validation_failed", json.GetProperty("code").GetString());
    }
}
