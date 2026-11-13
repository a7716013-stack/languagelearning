namespace LanguageLearning.Domain.Goals;

public static class GoalRules
{
    public const int MinDailyStudyMinutes = 10;
    public const int MaxDailyStudyMinutes = 600;

    /// <summary>考試日期最多可設定在幾年後。</summary>
    public const int MaxYearsAhead = 3;
}
