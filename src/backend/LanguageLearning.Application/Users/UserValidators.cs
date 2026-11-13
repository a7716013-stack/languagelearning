using FluentValidation;
using LanguageLearning.Application.Common.Time;
using LanguageLearning.Application.Common.Validation;

namespace LanguageLearning.Application.Users;

public sealed class UpdateProfileRequestValidator : AbstractValidator<UpdateProfileRequest>
{
    public UpdateProfileRequestValidator()
    {
        RuleFor(x => x.DisplayName).ValidDisplayName();
        RuleFor(x => x.TimeZoneId)
            .NotEmpty().WithMessage("請選擇時區。")
            .MaximumLength(64).WithMessage("時區代碼不正確。")
            .Must(TimeZoneHelper.IsValid).WithMessage("無法識別的時區。");
        RuleFor(x => x.UiTheme)
            .NotEmpty().WithMessage("請選擇介面主題。")
            .Must(theme => UiThemes.All.Contains(theme)).WithMessage("介面主題只能是 system、light 或 dark。");
    }
}
