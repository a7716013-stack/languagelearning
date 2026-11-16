using System.Text.RegularExpressions;
using LanguageLearning.Domain.Common;
using LanguageLearning.Domain.QuestionBank;

namespace LanguageLearning.Domain.Exams;

/// <summary>
/// 模擬考或閱讀測驗的試卷：依序引用已發布的題組版本。草稿可以調整；發布後不可修改，只能停用。
/// 操作前須載入 <see cref="Items"/>。
/// </summary>
public sealed partial class ExamPaper
{
    public const int CodeMaxLength = 20;
    public const int TitleMaxLength = 200;

    private readonly List<ExamPaperItem> _items = [];

    private ExamPaper()
    {
    }

    public Guid Id { get; private set; }

    public Guid ExamVersionId { get; private set; }

    public string Code { get; private set; } = string.Empty;

    public string Title { get; private set; } = string.Empty;

    public ExamPaperKind Kind { get; private set; }

    public ContentStatus Status { get; private set; }

    public int ReadingTimeLimitSeconds { get; private set; }

    /// <summary>聽力音檔總長（毫秒），發布時計算；閱讀測驗為 null。</summary>
    public int? ListeningDurationMs { get; private set; }

    public Guid DefaultScoringRuleId { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public Guid CreatedByUserId { get; private set; }

    public DateTime? PublishedAtUtc { get; private set; }

    public Guid? PublishedByUserId { get; private set; }

    public DateTime? RetiredAtUtc { get; private set; }

    public Guid? RetiredByUserId { get; private set; }

    public byte[] RowVersion { get; private set; } = [];

    public IReadOnlyCollection<ExamPaperItem> Items => _items;

    public int TotalQuestions => _items.Sum(i => i.QuestionCount);

    public static ExamPaper Create(
        string code,
        string title,
        ExamPaperKind kind,
        Guid examVersionId,
        Guid defaultScoringRuleId,
        Guid userId,
        DateTime nowUtc)
    {
        Guard.Utc(nowUtc, nameof(nowUtc));
        Guard.NotEmpty(examVersionId, nameof(examVersionId));
        Guard.NotEmpty(defaultScoringRuleId, nameof(defaultScoringRuleId));
        Guard.NotEmpty(userId, nameof(userId));

        var normalizedCode = (code ?? string.Empty).Trim().ToUpperInvariant();
        if (!CodePattern().IsMatch(normalizedCode))
        {
            throw new DomainException(
                "invalid_paper_code",
                $"試卷代號只能使用英文字母、數字與連字號，最多 {CodeMaxLength} 字。",
                "code");
        }

        var normalizedTitle = Guard.Optional(title)
            ?? throw new DomainException("paper_title_required", "請填寫試卷名稱。", "title");
        if (normalizedTitle.Length > TitleMaxLength)
        {
            throw new DomainException("paper_title_too_long", $"試卷名稱不可超過 {TitleMaxLength} 字。", "title");
        }

        return new ExamPaper
        {
            Id = Guid.CreateVersion7(nowUtc),
            ExamVersionId = examVersionId,
            Code = normalizedCode,
            Title = normalizedTitle,
            Kind = kind,
            Status = ContentStatus.Draft,
            ReadingTimeLimitSeconds = ToeicBlueprint.ReadingTimeLimitSeconds,
            DefaultScoringRuleId = defaultScoringRuleId,
            CreatedAtUtc = nowUtc,
            CreatedByUserId = userId,
        };
    }

    /// <summary>加入已發布的題組版本；試卷依 Part 排序，同一 Part 依加入順序。</summary>
    public ExamPaperItem AddGroup(QuestionGroupVersion version, QuestionType questionType, DateTime nowUtc)
    {
        ArgumentNullException.ThrowIfNull(version);
        ArgumentNullException.ThrowIfNull(questionType);
        EnsureDraft();

        if (version.Status != ContentStatus.Published)
        {
            throw new DomainException("version_not_published", "只能加入已發布的題組版本。", "groupVersionId");
        }

        if (Kind == ExamPaperKind.ReadingOnly && questionType.Section != ExamSection.Reading)
        {
            throw new DomainException("listening_not_allowed", "閱讀測驗只能加入 Part 5–7 的題組。", "groupVersionId");
        }

        if (_items.Any(i => i.QuestionGroupId == version.QuestionGroupId))
        {
            throw new DomainException("group_already_in_paper", "此題組已在試卷中。", "groupVersionId");
        }

        if (version.Questions.Count == 0)
        {
            throw new ArgumentException("題組版本須載入題目。", nameof(version));
        }

        var item = ExamPaperItem.Create(
            Id,
            version.QuestionGroupId,
            version.Id,
            questionType.Id,
            questionType.Part,
            version.Questions.Count,
            _items.Count + 1,
            nowUtc);
        _items.Add(item);
        Reorder();
        return item;
    }

    public void RemoveGroup(Guid groupVersionId)
    {
        EnsureDraft();
        var item = _items.FirstOrDefault(i => i.GroupVersionId == groupVersionId)
            ?? throw new DomainException("group_not_in_paper", "此題組不在試卷中。", "groupVersionId");
        _items.Remove(item);
        Reorder();
    }

    /// <summary>檢查題數是否符合 TOEIC 題型分布；回傳所有問題，空清單表示可以發布。</summary>
    public IReadOnlyList<ContentValidationError> Validate(IReadOnlyCollection<QuestionType> questionTypes)
    {
        ArgumentNullException.ThrowIfNull(questionTypes);
        var typesById = questionTypes.ToDictionary(t => t.Id);
        var errors = new List<ContentValidationError>();
        var expected = ToeicBlueprint.QuestionsPerType(Kind);

        var actual = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var item in _items)
        {
            if (!typesById.TryGetValue(item.QuestionTypeId, out var type))
            {
                throw new ArgumentException("缺少試卷使用的題型資料。", nameof(questionTypes));
            }

            actual[type.Code] = actual.GetValueOrDefault(type.Code) + item.QuestionCount;
        }

        foreach (var (code, count) in expected)
        {
            var current = actual.GetValueOrDefault(code);
            if (current != count)
            {
                var name = questionTypes.FirstOrDefault(t => t.Code == code)?.Name ?? code;
                errors.Add(new("invalid_part_question_count", $"{name} 須有 {count} 題，目前 {current} 題。"));
            }
        }

        foreach (var code in actual.Keys.Where(code => !expected.ContainsKey(code)))
        {
            errors.Add(new("unexpected_question_type", $"此試卷不應包含 {code}。"));
        }

        return errors;
    }

    public void Publish(
        IReadOnlyCollection<QuestionType> questionTypes,
        int? listeningDurationMs,
        Guid userId,
        DateTime nowUtc)
    {
        Guard.Utc(nowUtc, nameof(nowUtc));
        Guard.NotEmpty(userId, nameof(userId));
        EnsureDraft();

        var errors = Validate(questionTypes);
        if (errors.Count > 0)
        {
            throw new DomainException("exam_paper_invalid", string.Join(" ", errors.Select(e => e.Message)));
        }

        if (Kind == ExamPaperKind.FullTest && listeningDurationMs is not > 0)
        {
            throw new DomainException("listening_duration_required", "完整模擬考須有聽力音檔總長。");
        }

        Status = ContentStatus.Published;
        ListeningDurationMs = Kind == ExamPaperKind.FullTest ? listeningDurationMs : null;
        PublishedAtUtc = nowUtc;
        PublishedByUserId = userId;
    }

    public void Retire(Guid userId, DateTime nowUtc)
    {
        Guard.Utc(nowUtc, nameof(nowUtc));
        Guard.NotEmpty(userId, nameof(userId));
        if (Status != ContentStatus.Published)
        {
            throw new DomainException("paper_not_published", "只有已發布的試卷可以停用。");
        }

        Status = ContentStatus.Retired;
        RetiredAtUtc = nowUtc;
        RetiredByUserId = userId;
    }

    private void Reorder()
    {
        var order = 1;
        foreach (var item in _items.OrderBy(i => i.Part).ThenBy(i => i.SortOrder).ToList())
        {
            item.SetSortOrder(order++);
        }
    }

    private void EnsureDraft()
    {
        if (Status != ContentStatus.Draft)
        {
            throw new DomainException("paper_not_editable", "已發布或停用的試卷不可修改。");
        }
    }

    [GeneratedRegex("^[A-Z0-9][A-Z0-9-]{0,19}$")]
    private static partial Regex CodePattern();
}
