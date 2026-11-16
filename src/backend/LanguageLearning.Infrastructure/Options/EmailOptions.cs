using System.ComponentModel.DataAnnotations;
using MailKit.Security;

namespace LanguageLearning.Infrastructure.Options;

public enum EmailProvider
{
    /// <summary>以 SMTP 寄出（正式環境；CI 使用 Mailpit）。</summary>
    Smtp = 1,

    /// <summary>不寄出，存成 HTML 檔案（僅限本機開發）。</summary>
    File = 2,
}

public sealed class EmailOptions
{
    public const string SectionName = "Email";

    public EmailProvider Provider { get; set; } = EmailProvider.Smtp;

    [Required]
    public SmtpOptions Smtp { get; set; } = new();

    [Required]
    public FileEmailOptions File { get; set; } = new();

    [Required]
    public EmailOutboxOptions Outbox { get; set; } = new();
}

public sealed class SmtpOptions
{
    [Required]
    public string Host { get; set; } = "localhost";

    [Range(1, 65535)]
    public int Port { get; set; } = 1025;

    public SecureSocketOptions Security { get; set; } = SecureSocketOptions.None;

    public string? UserName { get; set; }

    public string? Password { get; set; }

    [Required]
    [EmailAddress]
    public string FromAddress { get; set; } = "no-reply@languagelearning.local";

    [Required]
    public string FromName { get; set; } = "LanguageLearning";
}

public sealed class FileEmailOptions
{
    /// <summary>信件檔存放資料夾；相對路徑以 API 專案的內容根目錄為基準。</summary>
    [Required]
    public string Directory { get; set; } = "App_Data/mail";
}

public sealed class EmailOutboxOptions
{
    /// <summary>背景服務輪詢寄件匣的間隔（新信件排入時會立即喚醒）。</summary>
    [Range(1, 3600)]
    public int PollIntervalSeconds { get; set; } = 15;

    /// <summary>每封信最多嘗試寄送的次數。</summary>
    [Range(1, 20)]
    public int MaxAttempts { get; set; } = 5;

    /// <summary>重試間隔的基準秒數；第 1～4 次失敗後分別等待基準的 1、5、15、30 倍。</summary>
    [Range(1, 3600)]
    public int RetryBaseDelaySeconds { get; set; } = 60;

    [Range(1, 200)]
    public int BatchSize { get; set; } = 20;

    /// <summary>處理中超過此秒數仍未完成（例如程式中途停止）的信件，會重新排入。</summary>
    [Range(30, 86400)]
    public int StaleProcessingSeconds { get; set; } = 300;
}
