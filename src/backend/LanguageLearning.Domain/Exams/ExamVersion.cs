namespace LanguageLearning.Domain.Exams;

/// <summary>
/// 考制版本（例如 TOEIC Listening &amp; Reading 現行題型）。後續 Phase 的試卷與計分規則會綁定版本。
/// </summary>
public sealed class ExamVersion
{
    public Guid Id { get; set; }

    public Guid ExamTypeId { get; set; }

    public ExamType? ExamType { get; set; }

    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public bool IsCurrent { get; set; }

    public DateOnly? EffectiveFrom { get; set; }
}
