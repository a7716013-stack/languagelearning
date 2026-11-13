using FluentValidation;
using LanguageLearning.Application.Common.Validation;

namespace LanguageLearning.Application.Auth;

public sealed class RegisterRequestValidator : AbstractValidator<RegisterRequest>
{
    public RegisterRequestValidator()
    {
        RuleFor(x => x.Email).ValidEmail();
        RuleFor(x => x.Password).StrongPassword();
        RuleFor(x => x.DisplayName).ValidDisplayName();
    }
}

public sealed class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.Email).ValidEmail();
        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("請輸入密碼。")
            .MaximumLength(ValidationRules.PasswordMaxLength).WithMessage("密碼長度不正確。");
    }
}

public sealed class ConfirmEmailRequestValidator : AbstractValidator<ConfirmEmailRequest>
{
    public ConfirmEmailRequestValidator()
    {
        RuleFor(x => x.UserId).NotEmpty().WithMessage("驗證連結不完整。");
        RuleFor(x => x.Token)
            .NotEmpty().WithMessage("驗證連結不完整。")
            .MaximumLength(2048).WithMessage("驗證連結不正確。");
    }
}

public sealed class ResendConfirmationRequestValidator : AbstractValidator<ResendConfirmationRequest>
{
    public ResendConfirmationRequestValidator()
    {
        RuleFor(x => x.Email).ValidEmail();
    }
}

public sealed class ForgotPasswordRequestValidator : AbstractValidator<ForgotPasswordRequest>
{
    public ForgotPasswordRequestValidator()
    {
        RuleFor(x => x.Email).ValidEmail();
    }
}

public sealed class ResetPasswordRequestValidator : AbstractValidator<ResetPasswordRequest>
{
    public ResetPasswordRequestValidator()
    {
        RuleFor(x => x.UserId).NotEmpty().WithMessage("重設連結不完整。");
        RuleFor(x => x.Token)
            .NotEmpty().WithMessage("重設連結不完整。")
            .MaximumLength(2048).WithMessage("重設連結不正確。");
        RuleFor(x => x.NewPassword).StrongPassword();
    }
}

public sealed class ChangePasswordRequestValidator : AbstractValidator<ChangePasswordRequest>
{
    public ChangePasswordRequestValidator()
    {
        RuleFor(x => x.CurrentPassword)
            .NotEmpty().WithMessage("請輸入目前的密碼。")
            .MaximumLength(ValidationRules.PasswordMaxLength).WithMessage("密碼長度不正確。");
        RuleFor(x => x.NewPassword)
            .StrongPassword()
            .NotEqual(x => x.CurrentPassword).WithMessage("新密碼不可與目前的密碼相同。");
    }
}
