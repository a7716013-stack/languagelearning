using System.Text.RegularExpressions;
using LanguageLearning.Domain.Common;

namespace LanguageLearning.Domain.QuestionBank;

/// <summary>
/// 題組的固定身分（ADR 0008）。內容存在 <see cref="QuestionGroupVersion"/>；
/// 每個題組最多一個草稿（資料庫篩選唯一索引）、最多一個已發布版本（Domain 規則加上題組的並行控制）。操作前須載入 <see cref="Versions"/>（含題目與選項）。
/// </summary>
public sealed partial class QuestionGroup
{
    private readonly List<QuestionGroupVersion> _versions = [];

    private QuestionGroup()
    {
    }

    public Guid Id { get; private set; }

    /// <summary>給人辨識的代號，例如 P5-0001。</summary>
    public string Code { get; private set; } = string.Empty;

    public Guid QuestionTypeId { get; private set; }

    public QuestionType? QuestionType { get; private set; }

    public Guid CreatedByUserId { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    /// <summary>
    /// 目前已發布的版本號（沒有則為 null）。發布與停用都會更新題組本身，
    /// 讓同一題組的並行發布因 <see cref="RowVersion"/> 衝突而只有一個成功。
    /// </summary>
    public int? PublishedVersionNumber { get; private set; }

    public byte[] RowVersion { get; private set; } = [];

    public IReadOnlyCollection<QuestionGroupVersion> Versions => _versions;

    public QuestionGroupVersion? Draft => _versions.FirstOrDefault(v => v.Status == ContentStatus.Draft);

    public QuestionGroupVersion? PublishedVersion => _versions.FirstOrDefault(v => v.Status == ContentStatus.Published);

    /// <summary>建立題組與第 1 版草稿。</summary>
    public static QuestionGroup Create(string code, QuestionType questionType, Guid createdByUserId, DateTime nowUtc)
    {
        ArgumentNullException.ThrowIfNull(questionType);
        Guard.Utc(nowUtc, nameof(nowUtc));
        Guard.NotEmpty(createdByUserId, nameof(createdByUserId));

        var normalizedCode = (code ?? string.Empty).Trim().ToUpperInvariant();
        if (!CodePattern().IsMatch(normalizedCode))
        {
            throw new DomainException(
                "invalid_group_code",
                $"題組代號只能使用英文字母、數字與連字號，最多 {QuestionBankRules.GroupCodeMaxLength} 字。",
                "code");
        }

        var group = new QuestionGroup
        {
            Id = Guid.CreateVersion7(nowUtc),
            Code = normalizedCode,
            QuestionTypeId = questionType.Id,
            QuestionType = questionType,
            CreatedByUserId = createdByUserId,
            CreatedAtUtc = nowUtc,
        };
        group._versions.Add(QuestionGroupVersion.CreateDraft(group.Id, 1, createdByUserId, nowUtc));
        return group;
    }

    /// <summary>複製已發布版本成新草稿，用來修改已發布的題目。</summary>
    public QuestionGroupVersion CreateDraftFromPublished(Guid userId, DateTime nowUtc)
    {
        if (Draft is not null)
        {
            throw new DomainException("draft_already_exists", "此題組已有草稿，請先發布或捨棄草稿。");
        }

        var published = PublishedVersion
            ?? throw new DomainException("no_published_version", "此題組沒有已發布的版本可以複製。");
        var draft = published.CopyAsDraft(NextVersionNumber(), userId, nowUtc);
        _versions.Add(draft);
        return draft;
    }

    /// <summary>發布草稿；原本已發布的版本同時改為停用。</summary>
    public QuestionGroupVersion Publish(QuestionType questionType, Guid userId, DateTime nowUtc)
    {
        ArgumentNullException.ThrowIfNull(questionType);
        if (questionType.Id != QuestionTypeId)
        {
            throw new ArgumentException("題型與題組不符。", nameof(questionType));
        }

        var draft = Draft ?? throw new DomainException("no_draft_version", "此題組沒有可以發布的草稿。");
        var errors = draft.Validate(questionType);
        if (errors.Count > 0)
        {
            throw new DomainException("question_group_invalid", string.Join(" ", errors.Select(e => e.Message)));
        }

        PublishedVersion?.MarkRetired(userId, nowUtc);
        draft.MarkPublished(userId, nowUtc);
        PublishedVersionNumber = draft.VersionNumber;
        return draft;
    }

    /// <summary>停用已發布版本（不再出現在新的練習與試卷中；已完成的作答不受影響）。</summary>
    public void Retire(Guid userId, DateTime nowUtc)
    {
        var published = PublishedVersion
            ?? throw new DomainException("no_published_version", "此題組沒有已發布的版本。");
        published.MarkRetired(userId, nowUtc);
        PublishedVersionNumber = null;
    }

    /// <summary>捨棄草稿。題組至少要保留一個版本，從未發布的題組請直接刪除題組。</summary>
    public void DiscardDraft()
    {
        var draft = Draft ?? throw new DomainException("no_draft_version", "此題組沒有草稿。");
        if (_versions.Count == 1)
        {
            throw new DomainException("cannot_discard_only_version", "這是題組唯一的版本，無法捨棄。");
        }

        _versions.Remove(draft);
    }

    private int NextVersionNumber() => _versions.Count == 0 ? 1 : _versions.Max(v => v.VersionNumber) + 1;

    [GeneratedRegex("^[A-Z0-9][A-Z0-9-]{0,19}$")]
    private static partial Regex CodePattern();
}
