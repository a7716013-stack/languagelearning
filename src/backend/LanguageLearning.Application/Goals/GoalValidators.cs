using FluentValidation;
using LanguageLearning.Domain.Goals;

namespace LanguageLearning.Application.Goals;

/// <summary>
/// 輸入格式驗證。依考試而異的分數範圍與「不可早於今天」等規則由 Domain（<see cref="UserExamGoal"/>）負責。
/// </summary>
public sealed class UpsertExamGoalRequestValidator : AbstractValidator<UpsertExamGoalRequest>
{
    public UpsertExamGoalRequestValidator()
    {
        RuleFor(x => x.TargetScore)
            .GreaterThanOrEqualTo(0).WithMessage("目標分數不可為負數。")
            .LessThanOrEqualTo(10000).WithMessage("目標分數不正確。");
        RuleFor(x => x.ExamDate)
            .NotEqual(default(DateOnly)).WithMessage("請選擇考試日期。");
        RuleFor(x => x.DailyStudyMinutes)
            .InclusiveBetween(GoalRules.MinDailyStudyMinutes, GoalRules.MaxDailyStudyMinutes)
            .WithMessage($"每日學習時間須介於 {GoalRules.MinDailyStudyMinutes} 到 {GoalRules.MaxDailyStudyMinutes} 分鐘之間。");
        RuleFor(x => x.Version)
            .MaximumLength(64).WithMessage("版本資訊不正確。");
    }
}
