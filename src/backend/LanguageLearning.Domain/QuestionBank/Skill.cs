using LanguageLearning.Domain.Exams;

namespace LanguageLearning.Domain.QuestionBank;

/// <summary>題目考的能力（參考資料）。<see cref="Section"/> 為 null 表示聽力與閱讀都適用。</summary>
public sealed class Skill
{
    public Guid Id { get; set; }

    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public ExamSection? Section { get; set; }

    public int SortOrder { get; set; }
}
