using LanguageLearning.Domain.QuestionBank;

namespace LanguageLearning.Domain.Exams;

public enum ExamPaperKind
{
    /// <summary>完整模擬考：聽力 Part 1–4 與閱讀 Part 5–7，共 200 題。</summary>
    FullTest = 1,

    /// <summary>閱讀測驗：Part 5–7 共 100 題（音檔補齊前使用）。</summary>
    ReadingOnly = 2,
}

/// <summary>
/// TOEIC L&amp;R 試卷的題型題數（2026-11-16 依 IIBC 測驗格式頁與 ETS 2018 題型更新說明查證）。
/// </summary>
public static class ToeicBlueprint
{
    public const int ReadingTimeLimitSeconds = 75 * 60;

    private static readonly Dictionary<string, int> FullTest = new(StringComparer.Ordinal)
    {
        [QuestionTypeCodes.Photographs] = 6,
        [QuestionTypeCodes.QuestionResponse] = 25,
        [QuestionTypeCodes.Conversations] = 39,
        [QuestionTypeCodes.Talks] = 30,
        [QuestionTypeCodes.IncompleteSentences] = 30,
        [QuestionTypeCodes.TextCompletion] = 16,
        [QuestionTypeCodes.SinglePassage] = 29,
        [QuestionTypeCodes.MultiplePassages] = 25,
    };

    private static readonly Dictionary<string, int> ReadingOnly = new(StringComparer.Ordinal)
    {
        [QuestionTypeCodes.IncompleteSentences] = 30,
        [QuestionTypeCodes.TextCompletion] = 16,
        [QuestionTypeCodes.SinglePassage] = 29,
        [QuestionTypeCodes.MultiplePassages] = 25,
    };

    /// <summary>各題型應有的題數（以題型代碼為鍵）。</summary>
    public static IReadOnlyDictionary<string, int> QuestionsPerType(ExamPaperKind kind) =>
        kind == ExamPaperKind.FullTest ? FullTest : ReadingOnly;
}
