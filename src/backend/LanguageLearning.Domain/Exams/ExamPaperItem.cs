namespace LanguageLearning.Domain.Exams;

/// <summary>
/// 試卷中的一個題組（引用特定的已發布版本）。題型、Part 與題數在加入時記錄，
/// 因為引用的版本不可變，這些值也不會改變。
/// </summary>
public sealed class ExamPaperItem
{
    private ExamPaperItem()
    {
    }

    public Guid Id { get; private set; }

    public Guid ExamPaperId { get; private set; }

    public Guid QuestionGroupId { get; private set; }

    public Guid GroupVersionId { get; private set; }

    public Guid QuestionTypeId { get; private set; }

    public int Part { get; private set; }

    public int QuestionCount { get; private set; }

    public int SortOrder { get; private set; }

    internal static ExamPaperItem Create(
        Guid examPaperId,
        Guid questionGroupId,
        Guid groupVersionId,
        Guid questionTypeId,
        int part,
        int questionCount,
        int sortOrder,
        DateTime nowUtc) =>
        new()
        {
            Id = Guid.CreateVersion7(nowUtc),
            ExamPaperId = examPaperId,
            QuestionGroupId = questionGroupId,
            GroupVersionId = groupVersionId,
            QuestionTypeId = questionTypeId,
            Part = part,
            QuestionCount = questionCount,
            SortOrder = sortOrder,
        };

    internal void SetSortOrder(int sortOrder) => SortOrder = sortOrder;
}
