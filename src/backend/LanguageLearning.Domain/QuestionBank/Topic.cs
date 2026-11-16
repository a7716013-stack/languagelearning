namespace LanguageLearning.Domain.QuestionBank;

/// <summary>題目情境主題（參考資料），例如辦公室、旅遊。</summary>
public sealed class Topic
{
    public Guid Id { get; set; }

    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public int SortOrder { get; set; }
}
