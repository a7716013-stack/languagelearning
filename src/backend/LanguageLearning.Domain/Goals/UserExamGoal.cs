using LanguageLearning.Domain.Common;
using LanguageLearning.Domain.Exams;

namespace LanguageLearning.Domain.Goals;

/// <summary>
/// 使用者對某一種檢定的學習目標。每位使用者每種考試最多一筆。
/// </summary>
public sealed class UserExamGoal
{
    private UserExamGoal()
    {
    }

    public Guid Id { get; private set; }

    public Guid UserId { get; private set; }

    public Guid ExamTypeId { get; private set; }

    public ExamType? ExamType { get; private set; }

    public decimal TargetScore { get; private set; }

    public DateOnly ExamDate { get; private set; }

    public int DailyStudyMinutes { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public DateTime UpdatedAtUtc { get; private set; }

    /// <summary>SQL Server rowversion，用於樂觀並行控制。</summary>
    public byte[] RowVersion { get; private set; } = [];

    public static UserExamGoal Create(
        Guid userId,
        ExamType examType,
        decimal targetScore,
        DateOnly examDate,
        int dailyStudyMinutes,
        DateOnly todayInUserTimeZone,
        DateTime nowUtc)
    {
        ArgumentNullException.ThrowIfNull(examType);
        if (userId == Guid.Empty)
        {
            throw new ArgumentException("UserId 不可為空。", nameof(userId));
        }

        EnsureUtc(nowUtc);
        Validate(examType, targetScore, examDate, dailyStudyMinutes, todayInUserTimeZone);

        return new UserExamGoal
        {
            Id = Guid.CreateVersion7(nowUtc),
            UserId = userId,
            ExamTypeId = examType.Id,
            ExamType = examType,
            TargetScore = targetScore,
            ExamDate = examDate,
            DailyStudyMinutes = dailyStudyMinutes,
            CreatedAtUtc = nowUtc,
            UpdatedAtUtc = nowUtc,
        };
    }

    public void Update(
        ExamType examType,
        decimal targetScore,
        DateOnly examDate,
        int dailyStudyMinutes,
        DateOnly todayInUserTimeZone,
        DateTime nowUtc)
    {
        ArgumentNullException.ThrowIfNull(examType);
        if (examType.Id != ExamTypeId)
        {
            throw new InvalidOperationException("不可變更學習目標的考試類型。");
        }

        EnsureUtc(nowUtc);
        Validate(examType, targetScore, examDate, dailyStudyMinutes, todayInUserTimeZone);

        TargetScore = targetScore;
        ExamDate = examDate;
        DailyStudyMinutes = dailyStudyMinutes;
        UpdatedAtUtc = nowUtc;
    }

    /// <summary>距離考試的天數（以使用者時區的「今天」計算；已過期為負數）。</summary>
    public int DaysUntilExam(DateOnly todayInUserTimeZone) => ExamDate.DayNumber - todayInUserTimeZone.DayNumber;

    private static void Validate(
        ExamType examType,
        decimal targetScore,
        DateOnly examDate,
        int dailyStudyMinutes,
        DateOnly today)
    {
        if (!examType.IsAvailable)
        {
            throw new DomainException(
                "exam_not_available",
                $"{examType.Name} 尚未開放，暫時無法設定學習目標。",
                "examTypeCode");
        }

        if (!examType.IsValidScore(targetScore))
        {
            throw new DomainException(
                "invalid_target_score",
                $"目標分數須介於 {examType.MinScore:0.#} 到 {examType.MaxScore:0.#} 之間，且以 {examType.ScoreStep:0.#} 分為單位。",
                "targetScore");
        }

        if (examDate < today)
        {
            throw new DomainException("invalid_exam_date", "考試日期不可早於今天。", "examDate");
        }

        if (examDate > today.AddYears(GoalRules.MaxYearsAhead))
        {
            throw new DomainException(
                "invalid_exam_date",
                $"考試日期不可超過 {GoalRules.MaxYearsAhead} 年後。",
                "examDate");
        }

        if (dailyStudyMinutes is < GoalRules.MinDailyStudyMinutes or > GoalRules.MaxDailyStudyMinutes)
        {
            throw new DomainException(
                "invalid_daily_minutes",
                $"每日學習時間須介於 {GoalRules.MinDailyStudyMinutes} 到 {GoalRules.MaxDailyStudyMinutes} 分鐘之間。",
                "dailyStudyMinutes");
        }
    }

    private static void EnsureUtc(DateTime value)
    {
        if (value.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException("時間必須為 UTC。", nameof(value));
        }
    }
}
