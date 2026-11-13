namespace LanguageLearning.Application.Users;

public sealed record CurrentUserDto(
    Guid Id,
    string Email,
    string DisplayName,
    IReadOnlyList<string> Roles,
    string TimeZoneId,
    string UiTheme,
    bool EmailConfirmed,
    bool OnboardingCompleted,
    DateTime CreatedAtUtc);

public sealed record UpdateProfileRequest(string DisplayName, string TimeZoneId, string UiTheme);

public static class UiThemes
{
    public const string System = "system";
    public const string Light = "light";
    public const string Dark = "dark";

    public static readonly IReadOnlyList<string> All = [System, Light, Dark];
}
