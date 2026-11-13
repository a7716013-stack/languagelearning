using System.ComponentModel.DataAnnotations;
using MailKit.Security;

namespace LanguageLearning.Infrastructure.Options;

public sealed class EmailOptions
{
    public const string SectionName = "Email";

    [Required]
    public SmtpOptions Smtp { get; set; } = new();
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
