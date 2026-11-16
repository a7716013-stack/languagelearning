namespace LanguageLearning.Domain.QuestionBank;

/// <summary>Part 6、7 的文章。Part 7 雙篇、三篇題組各有 2、3 篇。</summary>
public sealed class QuestionGroupPassage
{
    private QuestionGroupPassage()
    {
    }

    public Guid Id { get; private set; }

    public Guid GroupVersionId { get; private set; }

    public int SortOrder { get; private set; }

    /// <summary>文章類型標題，例如「E-mail」「Invoice」。</summary>
    public string? Title { get; private set; }

    public string Body { get; private set; } = string.Empty;

    internal static QuestionGroupPassage Create(Guid groupVersionId, int sortOrder, string? title, string body, DateTime nowUtc) =>
        new()
        {
            Id = Guid.CreateVersion7(nowUtc),
            GroupVersionId = groupVersionId,
            SortOrder = sortOrder,
            Title = title,
            Body = body,
        };

    internal void Update(string? title, string body)
    {
        Title = title;
        Body = body;
    }

    internal void SetSortOrder(int sortOrder) => SortOrder = sortOrder;
}
