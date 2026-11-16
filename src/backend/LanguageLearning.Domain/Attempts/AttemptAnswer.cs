namespace LanguageLearning.Domain.Attempts;

/// <summary>
/// 一題的作答。作答中可更新（依 <see cref="ClientSequence"/> 忽略較舊的送出），完成後不可變。
/// </summary>
public sealed class AttemptAnswer
{
    private AttemptAnswer()
    {
    }

    public Guid Id { get; private set; }

    public Guid AttemptId { get; private set; }

    public Guid SnapshotId { get; private set; }

    /// <summary>選擇的選項；null 表示清除作答。</summary>
    public Guid? SelectedOptionId { get; private set; }

    /// <summary>前端遞增的序號；多分頁同時作答時，以序號最大的為準。</summary>
    public long ClientSequence { get; private set; }

    public bool MarkedForReview { get; private set; }

    /// <summary>約略作答時間（毫秒），只供參考。</summary>
    public int? ElapsedMs { get; private set; }

    public DateTime UpdatedAtUtc { get; private set; }

    /// <summary>完成時寫入；作答中為 null。</summary>
    public bool? IsCorrect { get; private set; }

    internal static AttemptAnswer Create(
        Guid attemptId,
        Guid snapshotId,
        Guid? selectedOptionId,
        long clientSequence,
        bool markedForReview,
        int? elapsedMs,
        DateTime nowUtc) =>
        new()
        {
            Id = Guid.CreateVersion7(nowUtc),
            AttemptId = attemptId,
            SnapshotId = snapshotId,
            SelectedOptionId = selectedOptionId,
            ClientSequence = clientSequence,
            MarkedForReview = markedForReview,
            ElapsedMs = elapsedMs,
            UpdatedAtUtc = nowUtc,
        };

    internal void Update(Guid? selectedOptionId, long clientSequence, bool markedForReview, int? elapsedMs, DateTime nowUtc)
    {
        SelectedOptionId = selectedOptionId;
        ClientSequence = clientSequence;
        MarkedForReview = markedForReview;
        ElapsedMs = elapsedMs;
        UpdatedAtUtc = nowUtc;
    }

    internal void MarkScored(bool isCorrect) => IsCorrect = isCorrect;
}
