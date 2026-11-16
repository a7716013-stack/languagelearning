namespace LanguageLearning.Domain.QuestionBank;

public static class QuestionBankRules
{
    public const int GroupCodeMaxLength = 20;
    public const int StemMaxLength = 1000;
    public const int OptionTextMaxLength = 500;
    public const int PassageTitleMaxLength = 200;
    public const int AuthorNoteMaxLength = 1000;
    public const int AltTextMaxLength = 500;

    /// <summary>文章、腳本、解析的長度上限。</summary>
    public const int LongTextMaxLength = 20000;

    public const int MinOptions = 3;
    public const int MaxOptions = 4;

    /// <summary>選項代號，依序指派。</summary>
    public static readonly IReadOnlyList<string> OptionLabels = ["A", "B", "C", "D"];
}

/// <summary>發布前檢查出的問題，供管理介面一次列出。</summary>
public sealed record ContentValidationError(string Code, string Message);
