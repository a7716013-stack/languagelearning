using LanguageLearning.Application.Abstractions;
using LanguageLearning.Application.Common.Email;
using LanguageLearning.Infrastructure;
using LanguageLearning.Infrastructure.Email;
using LanguageLearning.Infrastructure.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace LanguageLearning.UnitTests.Infrastructure;

public sealed class EmailInfrastructureTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), $"ll-mail-{Guid.NewGuid():N}");

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task FileEmailSender_writes_html_file_with_header_and_body()
    {
        var sender = new FileEmailSender(
            Options.Create(new EmailOptions { Provider = EmailProvider.File, File = { Directory = "mail" } }),
            new FakeHostEnvironment(Environments.Development, _root),
            TimeProvider.System,
            NullLogger<FileEmailSender>.Instance);

        await sender.SendAsync(
            new EmailMessage(
                "student@example.com",
                "請驗證 -- 信箱",
                "<p><a href=\"http://localhost:5173/confirm-email?userId=1&amp;token=abc\">驗證</a></p>",
                "純文字"),
            Ct);

        var file = Assert.Single(Directory.GetFiles(Path.Combine(_root, "mail"), "*.html"));
        var content = await File.ReadAllTextAsync(file, Ct);
        Assert.StartsWith("<!--", content, StringComparison.Ordinal);
        Assert.Contains("To: student@example.com", content, StringComparison.Ordinal);
        // 主旨中的 "--" 會被替換，避免提早結束 HTML 註解。
        Assert.Contains("Subject: 請驗證 － 信箱", content, StringComparison.Ordinal);
        Assert.Contains("/confirm-email?userId=1&amp;token=abc", content, StringComparison.Ordinal);
        Assert.Equal(1, CountOccurrences(content, "-->"));
    }

    [Fact]
    public void File_provider_is_rejected_in_production()
    {
        using var provider = BuildServices(Environments.Production, EmailProvider.File);

        var error = Assert.Throws<OptionsValidationException>(
            () => provider.GetRequiredService<IOptions<EmailOptions>>().Value);
        Assert.Contains("Email:Provider=File", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void File_provider_is_allowed_in_development_and_selects_file_sender()
    {
        using var provider = BuildServices(Environments.Development, EmailProvider.File);

        Assert.Equal(EmailProvider.File, provider.GetRequiredService<IOptions<EmailOptions>>().Value.Provider);
        Assert.IsType<FileEmailSender>(provider.GetRequiredService<IEmailSender>());
    }

    [Fact]
    public void Outbox_options_out_of_range_are_rejected()
    {
        using var provider = BuildServices(
            Environments.Development,
            EmailProvider.Smtp,
            new Dictionary<string, string?> { ["Email:Outbox:RetryBaseDelaySeconds"] = "0" });

        Assert.Throws<OptionsValidationException>(
            () => provider.GetRequiredService<IOptions<EmailOptions>>().Value);
    }

    public void Dispose()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }

    private ServiceProvider BuildServices(
        string environmentName,
        EmailProvider emailProvider,
        Dictionary<string, string?>? extra = null)
    {
        var settings = new Dictionary<string, string?>
        {
            ["ConnectionStrings:Default"] = "Server=unused;Database=unused",
            ["App:PublicBaseUrl"] = "http://localhost:5173",
            ["Email:Provider"] = emailProvider.ToString(),
        };
        foreach (var (key, value) in extra ?? [])
        {
            settings[key] = value;
        }

        var configuration = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IHostEnvironment>(new FakeHostEnvironment(environmentName, _root));
        services.AddSingleton(TimeProvider.System);
        services.AddInfrastructure(configuration);
        return services.BuildServiceProvider();
    }

    private static int CountOccurrences(string text, string value)
    {
        var count = 0;
        for (var index = text.IndexOf(value, StringComparison.Ordinal);
             index >= 0;
             index = text.IndexOf(value, index + value.Length, StringComparison.Ordinal))
        {
            count++;
        }

        return count;
    }

    private sealed class FakeHostEnvironment(string environmentName, string contentRoot) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = environmentName;

        public string ApplicationName { get; set; } = "LanguageLearning.UnitTests";

        public string ContentRootPath { get; set; } = contentRoot;

        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
