using System.Net;
using System.Net.Http.Json;
using LanguageLearning.Application.Auth;
using LanguageLearning.IntegrationTests.Infrastructure;

namespace LanguageLearning.IntegrationTests;

public sealed class SecurityTests(ApiFixture fixture) : IClassFixture<ApiFixture>
{
    private ApiFactory Factory => fixture.Factory;

    [Fact]
    public async Task Post_without_csrf_token_is_rejected()
    {
        using var client = Factory.CreateTestClient();

        var response = await client.PostAsync(
            "/api/auth/login",
            new { email = TestUsers.NewEmail(), password = TestUsers.Password },
            withCsrf: false);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("csrf_invalid", await TestClient.ReadProblemCodeAsync(response));
    }

    [Fact]
    public async Task Anonymous_csrf_token_is_not_valid_after_login()
    {
        var email = await TestUsers.RegisterConfirmedAsync(Factory);
        using var client = Factory.CreateTestClient();
        await client.RefreshCsrfAsync();
        var anonymousToken = (await client.Http.GetFromJsonAsync<CsrfTokenResponse>(
            "/api/auth/csrf",
            TestContext.Current.CancellationToken))!.Token;

        await TestClient.EnsureStatusAsync(await client.LoginAsync(email, TestUsers.Password), HttpStatusCode.OK);

        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/users/me/onboarding/complete");
        request.Headers.Add("X-XSRF-TOKEN", anonymousToken);
        var response = await client.Http.SendAsync(request, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("csrf_invalid", await TestClient.ReadProblemCodeAsync(response));
    }

    [Fact]
    public async Task Student_cannot_access_admin_endpoints()
    {
        var (client, _) = await TestUsers.LoggedInAsync(Factory);
        using (client)
        {
            foreach (var url in new[] { "/api/admin/summary", "/api/admin/users", "/api/admin/audit-logs" })
            {
                var response = await client.GetAsync(url);
                Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
                Assert.Equal("forbidden", await TestClient.ReadProblemCodeAsync(response));
            }
        }
    }

    [Fact]
    public async Task Anonymous_cannot_access_admin_endpoints()
    {
        using var client = Factory.CreateTestClient();

        var response = await client.GetAsync("/api/admin/users");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Security_headers_are_present()
    {
        using var client = Factory.CreateTestClient();

        var response = await client.GetAsync("/api/version");

        Assert.Equal("nosniff", response.Headers.GetValues("X-Content-Type-Options").Single());
        Assert.Equal("DENY", response.Headers.GetValues("X-Frame-Options").Single());
        Assert.Contains("frame-ancestors 'none'", response.Headers.GetValues("Content-Security-Policy").Single(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Auth_cookie_is_http_only_and_same_site_strict()
    {
        var email = await TestUsers.RegisterConfirmedAsync(Factory);
        using var client = Factory.CreateTestClient();

        var login = await client.PostAsync("/api/auth/login", new { email, password = TestUsers.Password, rememberMe = false });

        await TestClient.EnsureStatusAsync(login, HttpStatusCode.OK);
        var cookie = login.Headers.GetValues("Set-Cookie").Single(value => value.StartsWith("ll.auth=", StringComparison.Ordinal));
        Assert.Contains("httponly", cookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("samesite=strict", cookie, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("expires=", cookie, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Logs_do_not_contain_passwords_tokens_or_emails()
    {
        var email = TestUsers.NewEmail();
        const string newPassword = "Logged1Secret";
        using var client = Factory.CreateTestClient();

        await client.PostAsync("/api/auth/register", new { email, password = TestUsers.Password, displayName = "日誌" });
        var confirm = await Factory.Emails.WaitForLinkQueryAsync(email, "/confirm-email");
        await client.PostAsync("/api/auth/confirm-email", new { userId = confirm["userId"], token = confirm["token"] });
        await client.LoginAsync(email, "WrongPass123");
        await client.LoginAsync(email, TestUsers.Password);
        await client.PostAsync("/api/auth/forgot-password", new { email });
        var reset = await Factory.Emails.WaitForLinkQueryAsync(email, "/reset-password");
        await client.PostAsync(
            "/api/auth/reset-password",
            new { userId = reset["userId"], token = reset["token"], newPassword });

        var logs = string.Join('\n', Factory.Logs.Entries);
        Assert.NotEmpty(Factory.Logs.Entries);
        Assert.DoesNotContain(TestUsers.Password, logs, StringComparison.Ordinal);
        Assert.DoesNotContain(newPassword, logs, StringComparison.Ordinal);
        Assert.DoesNotContain("WrongPass123", logs, StringComparison.Ordinal);
        Assert.DoesNotContain(confirm["token"], logs, StringComparison.Ordinal);
        Assert.DoesNotContain(reset["token"], logs, StringComparison.Ordinal);
        Assert.DoesNotContain(email, logs, StringComparison.OrdinalIgnoreCase);
    }
}

public sealed class RateLimitTests(RateLimitedApiFixture fixture) : IClassFixture<RateLimitedApiFixture>
{
    [Fact]
    public async Task Auth_endpoints_are_rate_limited()
    {
        using var client = fixture.Factory.CreateTestClient();
        var statuses = new List<HttpStatusCode>();

        for (var i = 0; i < 4; i++)
        {
            var response = await client.PostAsync(
                "/api/auth/login",
                new { email = TestUsers.NewEmail(), password = TestUsers.Password });
            statuses.Add(response.StatusCode);

            if (response.StatusCode == HttpStatusCode.TooManyRequests)
            {
                Assert.True(response.Headers.Contains("Retry-After"));
                Assert.Equal("rate_limited", await TestClient.ReadProblemCodeAsync(response));
            }
        }

        Assert.Equal(
            new[]
            {
                HttpStatusCode.Unauthorized,
                HttpStatusCode.Unauthorized,
                HttpStatusCode.Unauthorized,
                HttpStatusCode.TooManyRequests,
            },
            statuses);
    }
}
