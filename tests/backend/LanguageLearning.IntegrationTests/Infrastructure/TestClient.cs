using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using LanguageLearning.Application.Auth;

namespace LanguageLearning.IntegrationTests.Infrastructure;

/// <summary>包裝 HttpClient：自動處理 Cookie 與 CSRF Token（X-XSRF-TOKEN）。</summary>
public sealed class TestClient(HttpClient http) : IDisposable
{
    private string? _csrfToken;

    public HttpClient Http => http;

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public async Task RefreshCsrfAsync()
    {
        var response = await http.GetFromJsonAsync<CsrfTokenResponse>("/api/auth/csrf", Ct);
        _csrfToken = response?.Token ?? throw new InvalidOperationException("無法取得 CSRF Token。");
    }

    public Task<HttpResponseMessage> GetAsync(string url) => http.GetAsync(url, Ct);

    public async Task<T> GetJsonAsync<T>(string url)
    {
        using var response = await http.GetAsync(url, Ct);
        await EnsureStatusAsync(response, HttpStatusCode.OK);
        return (await response.Content.ReadFromJsonAsync<T>(Ct))!;
    }

    public Task<HttpResponseMessage> PostAsync(string url, object? body = null, bool withCsrf = true) =>
        SendAsync(HttpMethod.Post, url, body, withCsrf);

    public Task<HttpResponseMessage> PutAsync(string url, object? body, bool withCsrf = true) =>
        SendAsync(HttpMethod.Put, url, body, withCsrf);

    public async Task<HttpResponseMessage> LoginAsync(string email, string password, bool rememberMe = false)
    {
        var response = await PostAsync("/api/auth/login", new { email, password, rememberMe });

        // Token 綁定使用者身分，登入後需重新取得。
        await RefreshCsrfAsync();
        return response;
    }

    public async Task<HttpResponseMessage> LogoutAsync()
    {
        var response = await PostAsync("/api/auth/logout");
        await RefreshCsrfAsync();
        return response;
    }

    public static async Task EnsureStatusAsync(HttpResponseMessage response, HttpStatusCode expected)
    {
        if (response.StatusCode != expected)
        {
            var body = await response.Content.ReadAsStringAsync(Ct);
            Assert.Fail($"預期 HTTP {(int)expected}，實際為 {(int)response.StatusCode}。內容：{body}");
        }
    }

    public static async Task<JsonElement> ReadJsonAsync(HttpResponseMessage response)
    {
        var json = await response.Content.ReadAsStringAsync(Ct);
        using var document = JsonDocument.Parse(json);
        return document.RootElement.Clone();
    }

    public static async Task<string?> ReadProblemCodeAsync(HttpResponseMessage response)
    {
        var root = await ReadJsonAsync(response);
        return root.TryGetProperty("code", out var code) ? code.GetString() : null;
    }

    public void Dispose() => http.Dispose();

    private async Task<HttpResponseMessage> SendAsync(HttpMethod method, string url, object? body, bool withCsrf)
    {
        if (withCsrf && _csrfToken is null)
        {
            await RefreshCsrfAsync();
        }

        using var request = new HttpRequestMessage(method, url);
        if (body is not null)
        {
            request.Content = JsonContent.Create(body);
        }

        if (withCsrf)
        {
            request.Headers.Add("X-XSRF-TOKEN", _csrfToken);
        }

        return await http.SendAsync(request, Ct);
    }
}
