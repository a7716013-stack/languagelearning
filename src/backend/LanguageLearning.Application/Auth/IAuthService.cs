using LanguageLearning.Application.Common.Results;
using LanguageLearning.Application.Users;

namespace LanguageLearning.Application.Auth;

/// <summary>
/// 帳號驗證流程。為避免帳號列舉，註冊、重寄驗證信、忘記密碼不論 Email 是否存在都回傳成功。
/// </summary>
public interface IAuthService
{
    Task<Result> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken);

    Task<Result> ConfirmEmailAsync(ConfirmEmailRequest request, CancellationToken cancellationToken);

    Task<Result> ResendConfirmationAsync(ResendConfirmationRequest request, CancellationToken cancellationToken);

    Task<Result<CurrentUserDto>> LoginAsync(LoginRequest request, CancellationToken cancellationToken);

    Task<Result> LogoutAsync(CancellationToken cancellationToken);

    Task<Result> ForgotPasswordAsync(ForgotPasswordRequest request, CancellationToken cancellationToken);

    Task<Result> ResetPasswordAsync(ResetPasswordRequest request, CancellationToken cancellationToken);

    Task<Result> ChangePasswordAsync(ChangePasswordRequest request, CancellationToken cancellationToken);
}
