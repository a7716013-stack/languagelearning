using System.Net;

namespace LanguageLearning.IntegrationTests.Infrastructure;

public static class TestUsers
{
    public const string Password = "Student1Pass";

    public static string NewEmail() => $"student-{Guid.NewGuid():N}@example.com";

    /// <summary>註冊並完成 Email 驗證，回傳 Email。</summary>
    public static async Task<string> RegisterConfirmedAsync(ApiFactory factory, string? email = null, string password = Password)
    {
        email ??= NewEmail();
        using var client = factory.CreateTestClient();

        var register = await client.PostAsync("/api/auth/register", new { email, password, displayName = "測試學生" });
        await TestClient.EnsureStatusAsync(register, HttpStatusCode.Accepted);

        var query = await factory.Emails.WaitForLinkQueryAsync(email, "/confirm-email");
        var confirm = await client.PostAsync(
            "/api/auth/confirm-email",
            new { userId = query["userId"], token = query["token"] });
        await TestClient.EnsureStatusAsync(confirm, HttpStatusCode.OK);

        return email;
    }

    /// <summary>建立已驗證並登入的使用者。</summary>
    public static async Task<(TestClient Client, string Email)> LoggedInAsync(ApiFactory factory)
    {
        var email = await RegisterConfirmedAsync(factory);
        var client = factory.CreateTestClient();
        var login = await client.LoginAsync(email, Password);
        await TestClient.EnsureStatusAsync(login, HttpStatusCode.OK);
        return (client, email);
    }

    public static async Task<TestClient> AdminAsync(ApiFactory factory)
    {
        var client = factory.CreateTestClient();
        var login = await client.LoginAsync(ApiFactory.AdminEmail, ApiFactory.AdminPassword);
        await TestClient.EnsureStatusAsync(login, HttpStatusCode.OK);
        return client;
    }
}
