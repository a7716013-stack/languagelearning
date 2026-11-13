using System.Text;
using LanguageLearning.Application.Abstractions;
using LanguageLearning.Application.Auth;
using LanguageLearning.Application.Common.Auditing;
using LanguageLearning.Application.Common.Email;
using LanguageLearning.Application.Common.Results;
using LanguageLearning.Application.Common.Security;
using LanguageLearning.Application.Common.Time;
using LanguageLearning.Application.Users;
using LanguageLearning.Domain.Auditing;
using LanguageLearning.Infrastructure.Options;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LanguageLearning.Infrastructure.Identity;

internal sealed class AuthService(
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager,
    IEmailSender emailSender,
    IAuditLogger auditLogger,
    ICurrentUser currentUser,
    IOptions<AppOptions> appOptions,
    IOptions<AuthOptions> authOptions,
    TimeProvider timeProvider,
    ILogger<AuthService> logger) : IAuthService
{
    // 用於帳號不存在時仍執行一次密碼雜湊，降低以回應時間推測帳號是否存在的風險。
    private static readonly ApplicationUser TimingDummyUser = new() { UserName = "timing-dummy" };

    public async Task<Result> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken)
    {
        var email = request.Email.Trim();
        var displayName = request.DisplayName.Trim();

        var existing = await userManager.FindByEmailAsync(email);
        if (existing is not null)
        {
            var notice = existing.EmailConfirmed
                ? EmailTemplates.AccountAlreadyExists(
                    existing.Email!,
                    existing.DisplayName,
                    BuildLink("/login", new Dictionary<string, string>()))
                : await BuildConfirmationEmailAsync(existing);
            await SendSafelyAsync(notice, cancellationToken);
            await AuditAsync(AuditActions.RegisterDuplicate, true, existing.Id, cancellationToken);
            return Result.Success();
        }

        var user = new ApplicationUser
        {
            Id = Guid.CreateVersion7(timeProvider.GetUtcNow()),
            UserName = email,
            Email = email,
            DisplayName = displayName,
            TimeZoneId = TimeZoneHelper.DefaultTimeZoneId,
            UiTheme = UiThemes.System,
            CreatedAtUtc = timeProvider.GetUtcNow().UtcDateTime,
        };

        var created = await userManager.CreateAsync(user, request.Password);
        if (!created.Succeeded)
        {
            if (created.Errors.Any(e => e.Code is "DuplicateEmail" or "DuplicateUserName"))
            {
                // 同時註冊造成的競爭狀況：與「已存在」一樣回應成功。
                return Result.Success();
            }

            return ToValidationError(created.Errors, "password");
        }

        var roleResult = await userManager.AddToRoleAsync(user, Roles.Student);
        if (!roleResult.Succeeded)
        {
            throw new InvalidOperationException("無法指派 Student 角色，請確認資料庫 Migration 已套用。");
        }

        await SendSafelyAsync(await BuildConfirmationEmailAsync(user), cancellationToken);
        await AuditAsync(AuditActions.Register, true, user.Id, cancellationToken);
        return Result.Success();
    }

    public async Task<Result> ConfirmEmailAsync(ConfirmEmailRequest request, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(request.UserId.ToString());
        if (user is null)
        {
            await AuditAsync(AuditActions.EmailConfirmFailed, false, null, cancellationToken);
            return AuthErrors.InvalidConfirmationLink;
        }

        if (user.EmailConfirmed)
        {
            return Result.Success();
        }

        if (!TryDecodeToken(request.Token, out var token))
        {
            await AuditAsync(AuditActions.EmailConfirmFailed, false, user.Id, cancellationToken);
            return AuthErrors.InvalidConfirmationLink;
        }

        var result = await userManager.ConfirmEmailAsync(user, token);
        if (!result.Succeeded)
        {
            await AuditAsync(AuditActions.EmailConfirmFailed, false, user.Id, cancellationToken);
            return AuthErrors.InvalidConfirmationLink;
        }

        await AuditAsync(AuditActions.EmailConfirmed, true, user.Id, cancellationToken);
        return Result.Success();
    }

    public async Task<Result> ResendConfirmationAsync(
        ResendConfirmationRequest request,
        CancellationToken cancellationToken)
    {
        var user = await userManager.FindByEmailAsync(request.Email.Trim());
        if (user is not null && !user.EmailConfirmed)
        {
            await SendSafelyAsync(await BuildConfirmationEmailAsync(user), cancellationToken);
        }

        return Result.Success();
    }

    public async Task<Result<CurrentUserDto>> LoginAsync(LoginRequest request, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByEmailAsync(request.Email.Trim());
        if (user is null)
        {
            _ = userManager.PasswordHasher.HashPassword(TimingDummyUser, request.Password);
            await AuditAsync(
                AuditActions.LoginFailed,
                false,
                null,
                cancellationToken,
                new Dictionary<string, object?> { ["reason"] = "unknown_account" });
            return AuthErrors.InvalidCredentials;
        }

        if (await userManager.IsLockedOutAsync(user))
        {
            await AuditAsync(AuditActions.LoginLockedOut, false, user.Id, cancellationToken);
            return AuthErrors.AccountLocked;
        }

        // SignInManager 會在檢查密碼前先拒絕未驗證帳號，可能讓人推測帳號存在。
        // 因此先自行驗證密碼，只有密碼正確時才回應「尚未驗證 Email」。
        if (authOptions.Value.RequireConfirmedEmail && !user.EmailConfirmed)
        {
            if (!await userManager.CheckPasswordAsync(user, request.Password))
            {
                await userManager.AccessFailedAsync(user);
                await AuditAsync(AuditActions.LoginFailed, false, user.Id, cancellationToken);
                return await userManager.IsLockedOutAsync(user)
                    ? AuthErrors.AccountLocked
                    : AuthErrors.InvalidCredentials;
            }

            await AuditAsync(AuditActions.LoginNotConfirmed, false, user.Id, cancellationToken);
            return AuthErrors.EmailNotConfirmed;
        }

        var result = await signInManager.PasswordSignInAsync(
            user,
            request.Password,
            request.RememberMe,
            lockoutOnFailure: true);

        if (result.Succeeded)
        {
            user.LastLoginAtUtc = timeProvider.GetUtcNow().UtcDateTime;
            await userManager.UpdateAsync(user);
            await AuditAsync(AuditActions.LoginSucceeded, true, user.Id, cancellationToken);
            return await userManager.ToCurrentUserDtoAsync(user);
        }

        if (result.IsLockedOut)
        {
            await AuditAsync(AuditActions.LoginLockedOut, false, user.Id, cancellationToken);
            return AuthErrors.AccountLocked;
        }

        if (result.IsNotAllowed)
        {
            await AuditAsync(AuditActions.LoginNotConfirmed, false, user.Id, cancellationToken);
            return AuthErrors.EmailNotConfirmed;
        }

        await AuditAsync(AuditActions.LoginFailed, false, user.Id, cancellationToken);
        return AuthErrors.InvalidCredentials;
    }

    public async Task<Result> LogoutAsync(CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId;
        await signInManager.SignOutAsync();
        if (userId is not null)
        {
            await AuditAsync(AuditActions.Logout, true, userId, cancellationToken);
        }

        return Result.Success();
    }

    public async Task<Result> ForgotPasswordAsync(ForgotPasswordRequest request, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByEmailAsync(request.Email.Trim());
        if (user is null)
        {
            return Result.Success();
        }

        if (!user.EmailConfirmed)
        {
            // 尚未驗證的帳號改寄驗證信，驗證後即可登入或再重設密碼。
            await SendSafelyAsync(await BuildConfirmationEmailAsync(user), cancellationToken);
            return Result.Success();
        }

        var token = await userManager.GeneratePasswordResetTokenAsync(user);
        var link = BuildLink(
            "/reset-password",
            new Dictionary<string, string>
            {
                ["userId"] = user.Id.ToString(),
                ["token"] = EncodeToken(token),
            });
        await SendSafelyAsync(EmailTemplates.ResetPassword(user.Email!, user.DisplayName, link), cancellationToken);
        await AuditAsync(AuditActions.PasswordResetRequested, true, user.Id, cancellationToken);
        return Result.Success();
    }

    public async Task<Result> ResetPasswordAsync(ResetPasswordRequest request, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(request.UserId.ToString());
        if (user is null || !TryDecodeToken(request.Token, out var token))
        {
            await AuditAsync(AuditActions.PasswordResetFailed, false, user?.Id, cancellationToken);
            return AuthErrors.InvalidResetLink;
        }

        var result = await userManager.ResetPasswordAsync(user, token, request.NewPassword);
        if (!result.Succeeded)
        {
            await AuditAsync(AuditActions.PasswordResetFailed, false, user.Id, cancellationToken);
            return result.Errors.Any(e => e.Code == nameof(IdentityErrorDescriber.InvalidToken))
                ? AuthErrors.InvalidResetLink
                : ToValidationError(result.Errors, "newPassword");
        }

        // 能透過信件重設密碼即代表擁有此信箱，解除鎖定並清除失敗次數。
        await userManager.ResetAccessFailedCountAsync(user);
        await userManager.SetLockoutEndDateAsync(user, null);
        await AuditAsync(AuditActions.PasswordReset, true, user.Id, cancellationToken);
        return Result.Success();
    }

    public async Task<Result> ChangePasswordAsync(ChangePasswordRequest request, CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } userId)
        {
            return AuthErrors.NotAuthenticated;
        }

        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null)
        {
            return AuthErrors.UserNotFound;
        }

        var result = await userManager.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);
        if (!result.Succeeded)
        {
            return result.Errors.Any(e => e.Code == nameof(IdentityErrorDescriber.PasswordMismatch))
                ? AppError.Validation("invalid_current_password", "目前的密碼不正確。", "currentPassword")
                : ToValidationError(result.Errors, "newPassword");
        }

        // Security Stamp 已更新：重新簽發目前裝置的 Cookie，其他裝置的工作階段將在下次驗證時失效。
        await signInManager.RefreshSignInAsync(user);
        await AuditAsync(AuditActions.PasswordChanged, true, user.Id, cancellationToken);
        return Result.Success();
    }

    private async Task<EmailMessage> BuildConfirmationEmailAsync(ApplicationUser user)
    {
        var token = await userManager.GenerateEmailConfirmationTokenAsync(user);
        var link = BuildLink(
            "/confirm-email",
            new Dictionary<string, string>
            {
                ["userId"] = user.Id.ToString(),
                ["token"] = EncodeToken(token),
            });
        return EmailTemplates.ConfirmEmail(user.Email!, user.DisplayName, link);
    }

    private Uri BuildLink(string path, IReadOnlyDictionary<string, string> query) =>
        EmailTemplates.BuildLink(appOptions.Value.PublicBaseUrl, path, query);

    private async Task SendSafelyAsync(EmailMessage message, CancellationToken cancellationToken)
    {
        try
        {
            await emailSender.SendAsync(message, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // 不記錄收件者與連結（含 Token）。使用者可稍後重新寄送。
            logger.LogError(ex, "寄送系統信件失敗。Subject={Subject}", message.Subject);
        }
    }

    private Task AuditAsync(
        string action,
        bool succeeded,
        Guid? userId,
        CancellationToken cancellationToken,
        IReadOnlyDictionary<string, object?>? details = null) =>
        auditLogger.WriteAsync(
            new AuditEntry(action, succeeded, userId, "User", userId?.ToString(), details),
            cancellationToken);

    private static string EncodeToken(string token) =>
        WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));

    private static bool TryDecodeToken(string encoded, out string token)
    {
        token = string.Empty;
        try
        {
            token = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(encoded));
            return token.Length > 0;
        }
        catch (FormatException)
        {
            return false;
        }
    }

    private static AppError ToValidationError(IEnumerable<IdentityError> errors, string field)
    {
        var messages = errors.Select(e => e.Description).Distinct().ToArray();
        return AppError.Validation(
            "identity_validation_failed",
            messages.FirstOrDefault() ?? "資料驗證失敗。",
            new Dictionary<string, string[]> { [field] = messages });
    }
}
