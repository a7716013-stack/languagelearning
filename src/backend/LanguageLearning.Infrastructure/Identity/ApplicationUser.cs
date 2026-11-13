using LanguageLearning.Application.Common.Time;
using LanguageLearning.Application.Users;
using Microsoft.AspNetCore.Identity;

namespace LanguageLearning.Infrastructure.Identity;

public sealed class ApplicationUser : IdentityUser<Guid>
{
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>IANA 時區代碼，用於計算使用者的「今天」。</summary>
    public string TimeZoneId { get; set; } = TimeZoneHelper.DefaultTimeZoneId;

    public string UiTheme { get; set; } = UiThemes.System;

    public DateTime CreatedAtUtc { get; set; }

    public DateTime? LastLoginAtUtc { get; set; }

    public DateTime? OnboardingCompletedAtUtc { get; set; }
}
