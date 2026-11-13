using LanguageLearning.Application.Abstractions;
using LanguageLearning.Application.Auth;
using LanguageLearning.Application.Common.Auditing;
using LanguageLearning.Application.Common.Results;
using LanguageLearning.Application.Users;
using LanguageLearning.Domain.Auditing;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace LanguageLearning.Infrastructure.Identity;

internal sealed class UserProfileService(
    UserManager<ApplicationUser> userManager,
    ICurrentUser currentUser,
    IAppDbContext db,
    IAuditLogger auditLogger,
    TimeProvider timeProvider) : IUserProfileService
{
    public async Task<Result<CurrentUserDto>> GetCurrentAsync(CancellationToken cancellationToken)
    {
        var user = await FindCurrentUserAsync();
        if (user is null)
        {
            return AuthErrors.UserNotFound;
        }

        return await userManager.ToCurrentUserDtoAsync(user);
    }

    public async Task<Result<CurrentUserDto>> UpdateAsync(
        UpdateProfileRequest request,
        CancellationToken cancellationToken)
    {
        var user = await FindCurrentUserAsync();
        if (user is null)
        {
            return AuthErrors.UserNotFound;
        }

        user.DisplayName = request.DisplayName.Trim();
        user.TimeZoneId = request.TimeZoneId.Trim();
        user.UiTheme = request.UiTheme;

        var result = await userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            return result.Errors.Any(e => e.Code == nameof(IdentityErrorDescriber.ConcurrencyFailure))
                ? AppError.Conflict("profile_conflict", "個人資料已在其他地方更新，請重新整理後再試。")
                : AppError.Failure("profile_update_failed", "無法更新個人資料，請稍後再試。");
        }

        await auditLogger.WriteAsync(
            new AuditEntry(AuditActions.ProfileUpdated, true, user.Id, "User", user.Id.ToString()),
            cancellationToken);
        return await userManager.ToCurrentUserDtoAsync(user);
    }

    public async Task<Result<CurrentUserDto>> CompleteOnboardingAsync(CancellationToken cancellationToken)
    {
        var user = await FindCurrentUserAsync();
        if (user is null)
        {
            return AuthErrors.UserNotFound;
        }

        if (user.OnboardingCompletedAtUtc is not null)
        {
            return await userManager.ToCurrentUserDtoAsync(user);
        }

        var hasGoal = await db.UserExamGoals.AnyAsync(x => x.UserId == user.Id, cancellationToken);
        if (!hasGoal)
        {
            return AppError.Validation("goal_required", "請先設定至少一個學習目標。");
        }

        user.OnboardingCompletedAtUtc = timeProvider.GetUtcNow().UtcDateTime;
        var result = await userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            return AppError.Failure("onboarding_failed", "無法完成初次設定，請稍後再試。");
        }

        await auditLogger.WriteAsync(
            new AuditEntry(AuditActions.OnboardingCompleted, true, user.Id, "User", user.Id.ToString()),
            cancellationToken);
        return await userManager.ToCurrentUserDtoAsync(user);
    }

    private async Task<ApplicationUser?> FindCurrentUserAsync() =>
        currentUser.UserId is { } userId
            ? await userManager.FindByIdAsync(userId.ToString())
            : null;
}
