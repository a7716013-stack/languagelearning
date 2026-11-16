using LanguageLearning.Domain.Exams;

namespace LanguageLearning.Domain.QuestionBank;

/// <summary>
/// 題型與題組結構規則（參考資料，只由 Migration 修改）。發布題組時依此檢查題數、選項數與必要媒體。
/// </summary>
public sealed class QuestionType
{
    public Guid Id { get; set; }

    public Guid ExamVersionId { get; set; }

    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public int Part { get; set; }

    public ExamSection Section { get; set; }

    public int MinQuestions { get; set; }

    public int MaxQuestions { get; set; }

    /// <summary>每題的選項數（Part 2 為 3，其他為 4）。</summary>
    public int OptionCount { get; set; }

    public int MinPassages { get; set; }

    public int MaxPassages { get; set; }

    public bool RequiresAudio { get; set; }

    public bool RequiresPhoto { get; set; }

    public bool AllowsGraphic { get; set; }

    /// <summary>題目是否必須有題幹文字（Part 1、2、6 沒有）。</summary>
    public bool RequiresStem { get; set; }

    /// <summary>作答時是否顯示選項文字（Part 1、2 只播音）。</summary>
    public bool ShowsOptionText { get; set; }

    public int SortOrder { get; set; }
}
