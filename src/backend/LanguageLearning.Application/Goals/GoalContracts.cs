namespace LanguageLearning.Application.Goals;

public sealed record ExamGoalDto(
    string ExamTypeCode,
    string ExamTypeName,
    decimal TargetScore,
    DateOnly ExamDate,
    int DailyStudyMinutes,
    int DaysUntilExam,
    DateTime UpdatedAtUtc,
    string Version);

/// <param name="Version">更新既有目標時必須帶入取得時的 Version（樂觀並行控制）；新增時為 null。</param>
public sealed record UpsertExamGoalRequest(
    decimal TargetScore,
    DateOnly ExamDate,
    int DailyStudyMinutes,
    string? Version);
