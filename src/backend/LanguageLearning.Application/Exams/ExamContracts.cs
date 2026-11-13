namespace LanguageLearning.Application.Exams;

public sealed record ExamTypeDto(
    string Code,
    string Name,
    string Description,
    bool IsAvailable,
    decimal MinScore,
    decimal MaxScore,
    decimal ScoreStep);

public sealed record ExamVersionDto(
    string Code,
    string ExamTypeCode,
    string Name,
    string Description,
    bool IsCurrent,
    DateOnly? EffectiveFrom);
