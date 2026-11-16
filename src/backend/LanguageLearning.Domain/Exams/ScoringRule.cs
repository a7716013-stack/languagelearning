namespace LanguageLearning.Domain.Exams;

public enum ScoringRuleKind
{
    /// <summary>只計答對題數與答對率（Phase 2 唯一的規則）。</summary>
    RawCount = 1,
}

/// <summary>
/// 計分規則（參考資料，不可修改；新規則以新版本新增）。作答開始時記錄使用的規則，成績不回頭重算。
/// </summary>
public sealed class ScoringRule
{
    public Guid Id { get; set; }

    public Guid ExamVersionId { get; set; }

    public string Code { get; set; } = string.Empty;

    public int Version { get; set; }

    public ScoringRuleKind Kind { get; set; }

    /// <summary>規則參數（JSON）；保留給日後的非官方預估分數。</summary>
    public string? ParametersJson { get; set; }

    public string Description { get; set; } = string.Empty;

    public bool IsDefault { get; set; }
}
