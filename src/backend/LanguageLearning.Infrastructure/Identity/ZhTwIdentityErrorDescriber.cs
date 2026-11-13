using Microsoft.AspNetCore.Identity;

namespace LanguageLearning.Infrastructure.Identity;

/// <summary>Identity 錯誤訊息繁體中文化（保留原本的錯誤代碼）。</summary>
public sealed class ZhTwIdentityErrorDescriber : IdentityErrorDescriber
{
    public override IdentityError DefaultError() => With(base.DefaultError(), "發生未知錯誤，請稍後再試。");

    public override IdentityError ConcurrencyFailure() => With(base.ConcurrencyFailure(), "資料已被其他操作更新，請重新整理後再試。");

    public override IdentityError PasswordMismatch() => With(base.PasswordMismatch(), "密碼不正確。");

    public override IdentityError InvalidToken() => With(base.InvalidToken(), "連結無效或已過期。");

    public override IdentityError LoginAlreadyAssociated() => With(base.LoginAlreadyAssociated(), "此外部登入已綁定其他帳號。");

    public override IdentityError InvalidUserName(string? userName) => With(base.InvalidUserName(userName), "帳號名稱格式不正確。");

    public override IdentityError InvalidEmail(string? email) => With(base.InvalidEmail(email), "Email 格式不正確。");

    public override IdentityError DuplicateUserName(string userName) => With(base.DuplicateUserName(userName), "此帳號已被使用。");

    public override IdentityError DuplicateEmail(string email) => With(base.DuplicateEmail(email), "此 Email 已被使用。");

    public override IdentityError InvalidRoleName(string? role) => With(base.InvalidRoleName(role), "角色名稱不正確。");

    public override IdentityError DuplicateRoleName(string role) => With(base.DuplicateRoleName(role), "角色名稱已存在。");

    public override IdentityError UserAlreadyHasPassword() => With(base.UserAlreadyHasPassword(), "此帳號已設定密碼。");

    public override IdentityError UserLockoutNotEnabled() => With(base.UserLockoutNotEnabled(), "此帳號未啟用鎖定功能。");

    public override IdentityError UserAlreadyInRole(string role) => With(base.UserAlreadyInRole(role), "使用者已具有此角色。");

    public override IdentityError UserNotInRole(string role) => With(base.UserNotInRole(role), "使用者不具有此角色。");

    public override IdentityError PasswordTooShort(int length) => With(base.PasswordTooShort(length), $"密碼至少需要 {length} 個字元。");

    public override IdentityError PasswordRequiresUniqueChars(int uniqueChars) =>
        With(base.PasswordRequiresUniqueChars(uniqueChars), $"密碼至少需要 {uniqueChars} 個不同的字元。");

    public override IdentityError PasswordRequiresNonAlphanumeric() =>
        With(base.PasswordRequiresNonAlphanumeric(), "密碼需包含至少一個符號。");

    public override IdentityError PasswordRequiresDigit() => With(base.PasswordRequiresDigit(), "密碼需包含至少一個數字。");

    public override IdentityError PasswordRequiresLower() => With(base.PasswordRequiresLower(), "密碼需包含至少一個小寫英文字母。");

    public override IdentityError PasswordRequiresUpper() => With(base.PasswordRequiresUpper(), "密碼需包含至少一個大寫英文字母。");

    public override IdentityError RecoveryCodeRedemptionFailed() => With(base.RecoveryCodeRedemptionFailed(), "復原碼無效。");

    private static IdentityError With(IdentityError error, string description)
    {
        error.Description = description;
        return error;
    }
}
