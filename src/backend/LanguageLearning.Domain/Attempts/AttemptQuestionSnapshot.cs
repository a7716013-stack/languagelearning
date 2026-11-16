using LanguageLearning.Domain.Exams;

namespace LanguageLearning.Domain.Attempts;

/// <summary>開始作答時要用的一題（由 Application 從已發布的題組版本整理）。</summary>
public sealed record AttemptQuestionInfo(
    Guid QuestionGroupId,
    Guid GroupVersionId,
    Guid QuestionId,
    int Part,
    ExamSection Section,
    Guid CorrectOptionId);

/// <summary>
/// 作答當時的題目（ADR 0008）：只保存題組版本、題目、順序與當時的正確答案，建立後不可變。
/// </summary>
public sealed class AttemptQuestionSnapshot
{
    private AttemptQuestionSnapshot()
    {
    }

    public Guid Id { get; private set; }

    public Guid AttemptId { get; private set; }

    /// <summary>作答順序，從 1 開始。</summary>
    public int Sequence { get; private set; }

    /// <summary>冗餘保存題組身分，用來查詢使用者做過的題目。</summary>
    public Guid QuestionGroupId { get; private set; }

    public Guid GroupVersionId { get; private set; }

    public Guid QuestionId { get; private set; }

    public int Part { get; private set; }

    public ExamSection Section { get; private set; }

    public Guid CorrectOptionId { get; private set; }

    internal static AttemptQuestionSnapshot Create(Guid attemptId, int sequence, AttemptQuestionInfo info, DateTime nowUtc) =>
        new()
        {
            Id = Guid.CreateVersion7(nowUtc),
            AttemptId = attemptId,
            Sequence = sequence,
            QuestionGroupId = info.QuestionGroupId,
            GroupVersionId = info.GroupVersionId,
            QuestionId = info.QuestionId,
            Part = info.Part,
            Section = info.Section,
            CorrectOptionId = info.CorrectOptionId,
        };
}
