using LanguageLearning.Application.Abstractions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Serilog.Core;

namespace LanguageLearning.IntegrationTests.Infrastructure;

public sealed class ApiFactory(string connectionString, IReadOnlyDictionary<string, string?> overrides)
    : WebApplicationFactory<Program>
{
    public const string AdminEmail = "admin@languagelearning.test";
    public const string AdminPassword = "AdminPassw0rd";

    public CapturingEmailSender Emails { get; } = new();

    public CapturingLogSink Logs { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        var settings = new Dictionary<string, string?>
        {
            ["ConnectionStrings:Default"] = connectionString,
            ["Database:ApplyMigrationsOnStartup"] = "true",
            ["App:PublicBaseUrl"] = "http://localhost:5173",
            ["App:UseHttpsRedirection"] = "false",
            ["Auth:SecurityStampValidationMinutes"] = "0",
            ["RateLimiting:Auth:PermitLimit"] = "100000",
            ["RateLimiting:Api:PermitLimit"] = "100000",
            ["Seed:Admin:Email"] = AdminEmail,
            ["Seed:Admin:Password"] = AdminPassword,
            ["Seed:Admin:DisplayName"] = "測試管理員",
        };

        foreach (var (key, value) in overrides)
        {
            settings[key] = value;
        }

        foreach (var (key, value) in settings)
        {
            builder.UseSetting(key, value);
        }

        builder.ConfigureTestServices(services =>
        {
            services.AddSingleton<ILogEventSink>(Logs);
            services.RemoveAll<IEmailSender>();
            services.AddSingleton<IEmailSender>(Emails);
        });
    }

    public TestClient CreateTestClient() =>
        new(CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true,
            BaseAddress = new Uri("http://localhost"),
        }));
}
