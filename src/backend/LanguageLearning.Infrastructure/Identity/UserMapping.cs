using LanguageLearning.Application.Users;
using Microsoft.AspNetCore.Identity;

namespace LanguageLearning.Infrastructure.Identity;

internal static class UserMapping
{
    public static async Task<CurrentUserDto> ToCurrentUserDtoAsync(
        this UserManager<ApplicationUser> userManager,
        ApplicationUser user)
    {
        var roles = await userManager.GetRolesAsync(user);
        return new CurrentUserDto(
            user.Id,
            user.Email ?? string.Empty,
            user.DisplayName,
            roles.OrderBy(x => x, StringComparer.Ordinal).ToList(),
            user.TimeZoneId,
            user.UiTheme,
            user.EmailConfirmed,
            user.OnboardingCompletedAtUtc is not null,
            user.CreatedAtUtc);
    }
}
