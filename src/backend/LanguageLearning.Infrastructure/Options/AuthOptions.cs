using System.ComponentModel.DataAnnotations;

namespace LanguageLearning.Infrastructure.Options;

public sealed class AuthOptions
{
    public const string SectionName = "Auth";

    public bool RequireConfirmedEmail { get; set; } = true;

    [Range(1, 50)]
    public int MaxFailedAccessAttempts { get; set; } = 5;

    [Range(1, 1440)]
    public int LockoutMinutes { get; set; } = 15;

    /// <summary>登入 Cookie 有效時間（滑動延長）。</summary>
    [Range(5, 43200)]
    public int CookieExpirationMinutes { get; set; } = 10080;

    /// <summary>Security Stamp 重新驗證間隔；設為 0 代表每個請求都驗證。</summary>
    [Range(0, 1440)]
    public int SecurityStampValidationMinutes { get; set; } = 5;

    /// <summary>Email 驗證與重設密碼連結的有效時間。</summary>
    [Range(1, 168)]
    public int TokenLifespanHours { get; set; } = 24;
}
