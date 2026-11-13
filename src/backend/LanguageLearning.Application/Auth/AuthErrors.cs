using LanguageLearning.Application.Common.Results;

namespace LanguageLearning.Application.Auth;

public static class AuthErrors
{
    public static readonly AppError InvalidCredentials =
        AppError.Unauthorized("invalid_credentials", "Email 或密碼不正確。");

    public static readonly AppError AccountLocked =
        AppError.Locked("account_locked", "登入失敗次數過多，帳號已暫時鎖定，請稍後再試。");

    public static readonly AppError EmailNotConfirmed =
        AppError.Forbidden("email_not_confirmed", "此帳號尚未完成 Email 驗證，請先到信箱點擊驗證連結。");

    public static readonly AppError InvalidConfirmationLink =
        AppError.Validation("invalid_confirmation_link", "驗證連結無效或已過期，請重新寄送驗證信。");

    public static readonly AppError InvalidResetLink =
        AppError.Validation("invalid_reset_link", "重設密碼連結無效或已過期，請重新申請。");

    public static readonly AppError NotAuthenticated =
        AppError.Unauthorized("not_authenticated", "請先登入。");

    public static readonly AppError UserNotFound =
        AppError.Unauthorized("not_authenticated", "登入狀態已失效，請重新登入。");
}
