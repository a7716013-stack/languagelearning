using System.Globalization;
using System.Text;
using LanguageLearning.Application.Abstractions;
using LanguageLearning.Application.Common.Email;
using LanguageLearning.Infrastructure.Options;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LanguageLearning.Infrastructure.Email;

/// <summary>
/// 本機開發用：不寄出信件，改存成 HTML 檔案，用瀏覽器開啟即可點擊驗證或重設連結。
/// 正式環境禁止使用（設定驗證會讓程式無法啟動）。
/// </summary>
internal sealed class FileEmailSender(
    IOptions<EmailOptions> options,
    IHostEnvironment environment,
    TimeProvider timeProvider,
    ILogger<FileEmailSender> logger) : IEmailSender
{
    public async Task SendAsync(EmailMessage message, CancellationToken cancellationToken)
    {
        var configured = options.Value.File.Directory;
        var directory = Path.IsPathRooted(configured)
            ? configured
            : Path.GetFullPath(Path.Combine(environment.ContentRootPath, configured));
        Directory.CreateDirectory(directory);

        var now = timeProvider.GetUtcNow();
        var fileName = string.Create(
            CultureInfo.InvariantCulture,
            $"{now.UtcDateTime:yyyyMMdd-HHmmss-fff}-{Guid.NewGuid().ToString("N")[..8]}.html");

        var header = new StringBuilder()
            .AppendLine("<!--")
            .AppendLine("LanguageLearning 開發環境信件檔：未實際寄出。")
            .AppendLine($"To: {Sanitize(message.To)}")
            .AppendLine($"Subject: {Sanitize(message.Subject)}")
            .AppendLine(string.Create(CultureInfo.InvariantCulture, $"CreatedAtUtc: {now.UtcDateTime:O}"))
            .AppendLine("-->")
            .ToString();

        await File.WriteAllTextAsync(
            Path.Combine(directory, fileName),
            header + message.HtmlBody,
            new UTF8Encoding(encoderShouldEmitUTF8Identifier: false),
            cancellationToken);

        // 只記錄檔名，不記錄收件者與內容（內容含驗證連結）。
        logger.LogInformation("已將系統信件存成本機檔案（未實際寄出）。File={FileName}", fileName);
    }

    private static string Sanitize(string value) =>
        value.Replace("--", "－", StringComparison.Ordinal).Replace('\r', ' ').Replace('\n', ' ');
}
