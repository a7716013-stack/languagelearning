using FluentValidation;

namespace LanguageLearning.Application.Common.Validation;

public static class ValidationRules
{
    public const int EmailMaxLength = 256;
    public const int PasswordMinLength = 10;
    public const int PasswordMaxLength = 128;
    public const int DisplayNameMaxLength = 50;

    public static IRuleBuilderOptions<T, string> ValidEmail<T>(this IRuleBuilder<T, string> rule) =>
        rule
            .NotEmpty().WithMessage("請輸入 Email。")
            .MaximumLength(EmailMaxLength).WithMessage($"Email 長度不可超過 {EmailMaxLength} 字元。")
            .EmailAddress().WithMessage("Email 格式不正確。");

    /// <summary>新密碼規則：至少 10 碼，包含大寫、小寫英文字母與數字。</summary>
    public static IRuleBuilderOptions<T, string> StrongPassword<T>(this IRuleBuilder<T, string> rule) =>
        rule
            .NotEmpty().WithMessage("請輸入密碼。")
            .MinimumLength(PasswordMinLength).WithMessage($"密碼至少需要 {PasswordMinLength} 個字元。")
            .MaximumLength(PasswordMaxLength).WithMessage($"密碼長度不可超過 {PasswordMaxLength} 字元。")
            .Matches("[A-Z]").WithMessage("密碼需包含至少一個大寫英文字母。")
            .Matches("[a-z]").WithMessage("密碼需包含至少一個小寫英文字母。")
            .Matches("[0-9]").WithMessage("密碼需包含至少一個數字。");

    public static IRuleBuilderOptions<T, string> ValidDisplayName<T>(this IRuleBuilder<T, string> rule) =>
        rule
            .Must(value => !string.IsNullOrWhiteSpace(value)).WithMessage("請輸入顯示名稱。")
            .MaximumLength(DisplayNameMaxLength).WithMessage($"顯示名稱不可超過 {DisplayNameMaxLength} 字元。");
}
