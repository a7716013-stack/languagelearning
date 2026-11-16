using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using LanguageLearning.Domain.Common;

namespace LanguageLearning.Domain.QuestionBank;

/// <summary>
/// 題組的一個內容版本（ADR 0008）。只有草稿可以修改；發布後內容凍結，之後只能停用。
/// </summary>
public sealed class QuestionGroupVersion
{
    private const int MaxPassagesPerGroup = 3;
    private const int MaxQuestionsPerGroup = 5;

    private readonly List<QuestionGroupPassage> _passages = [];
    private readonly List<Question> _questions = [];
    private readonly List<QuestionGroupMedia> _media = [];

    private QuestionGroupVersion()
    {
    }

    public Guid Id { get; private set; }

    public Guid QuestionGroupId { get; private set; }

    public int VersionNumber { get; private set; }

    public ContentStatus Status { get; private set; }

    /// <summary>聽力腳本；作答中不回傳給學生。</summary>
    public string? Transcript { get; private set; }

    /// <summary>題組層級的解析或翻譯。</summary>
    public string? Explanation { get; private set; }

    public ContentSource Source { get; private set; }

    /// <summary>出題與審核備註，只給管理者看。</summary>
    public string? AuthorNote { get; private set; }

    /// <summary>發布時計算的內容雜湊，用來比較兩個版本是否相同。</summary>
    public string? ContentHash { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public Guid CreatedByUserId { get; private set; }

    public DateTime? PublishedAtUtc { get; private set; }

    public Guid? PublishedByUserId { get; private set; }

    public DateTime? RetiredAtUtc { get; private set; }

    public Guid? RetiredByUserId { get; private set; }

    public byte[] RowVersion { get; private set; } = [];

    public IReadOnlyCollection<QuestionGroupPassage> Passages => _passages;

    public IReadOnlyCollection<Question> Questions => _questions;

    public IReadOnlyCollection<QuestionGroupMedia> Media => _media;

    public bool IsDraft => Status == ContentStatus.Draft;

    internal static QuestionGroupVersion CreateDraft(Guid questionGroupId, int versionNumber, Guid userId, DateTime nowUtc)
    {
        Guard.Utc(nowUtc, nameof(nowUtc));
        Guard.NotEmpty(userId, nameof(userId));

        return new QuestionGroupVersion
        {
            Id = Guid.CreateVersion7(nowUtc),
            QuestionGroupId = questionGroupId,
            VersionNumber = versionNumber,
            Status = ContentStatus.Draft,
            Source = ContentSource.Original,
            CreatedAtUtc = nowUtc,
            CreatedByUserId = userId,
        };
    }

    public void UpdateContent(string? transcript, string? explanation, string? authorNote)
    {
        EnsureDraft();
        var normalizedTranscript = Guard.Optional(transcript);
        var normalizedExplanation = Guard.Optional(explanation);
        var normalizedNote = Guard.Optional(authorNote);

        if (normalizedTranscript is { Length: > QuestionBankRules.LongTextMaxLength })
        {
            throw new DomainException("transcript_too_long", "聽力腳本過長。", "transcript");
        }

        if (normalizedExplanation is { Length: > QuestionBankRules.LongTextMaxLength })
        {
            throw new DomainException("explanation_too_long", "解析過長。", "explanation");
        }

        if (normalizedNote is { Length: > QuestionBankRules.AuthorNoteMaxLength })
        {
            throw new DomainException(
                "author_note_too_long",
                $"備註不可超過 {QuestionBankRules.AuthorNoteMaxLength} 字。",
                "authorNote");
        }

        Transcript = normalizedTranscript;
        Explanation = normalizedExplanation;
        AuthorNote = normalizedNote;
    }

    public QuestionGroupPassage AddPassage(string? title, string body, DateTime nowUtc)
    {
        EnsureDraft();
        if (_passages.Count >= MaxPassagesPerGroup)
        {
            throw new DomainException("too_many_passages", $"每個題組最多 {MaxPassagesPerGroup} 篇文章。", "passages");
        }

        var (normalizedTitle, normalizedBody) = NormalizePassage(title, body);
        var passage = QuestionGroupPassage.Create(Id, _passages.Count + 1, normalizedTitle, normalizedBody, nowUtc);
        _passages.Add(passage);
        return passage;
    }

    public void UpdatePassage(Guid passageId, string? title, string body)
    {
        EnsureDraft();
        var (normalizedTitle, normalizedBody) = NormalizePassage(title, body);
        FindPassage(passageId).Update(normalizedTitle, normalizedBody);
    }

    public void RemovePassage(Guid passageId)
    {
        EnsureDraft();
        _passages.Remove(FindPassage(passageId));
        var order = 1;
        foreach (var passage in _passages.OrderBy(p => p.SortOrder))
        {
            passage.SetSortOrder(order++);
        }
    }

    public Question AddQuestion(QuestionInput input, DateTime nowUtc)
    {
        EnsureDraft();
        if (_questions.Count >= MaxQuestionsPerGroup)
        {
            throw new DomainException("too_many_questions", $"每個題組最多 {MaxQuestionsPerGroup} 題。", "questions");
        }

        var question = Question.Create(Id, _questions.Count + 1, input, nowUtc);
        _questions.Add(question);
        return question;
    }

    public void UpdateQuestion(Guid questionId, QuestionInput input, DateTime nowUtc)
    {
        EnsureDraft();
        FindQuestion(questionId).Apply(input, nowUtc);
    }

    public void RemoveQuestion(Guid questionId)
    {
        EnsureDraft();
        _questions.Remove(FindQuestion(questionId));
        var number = 1;
        foreach (var question in _questions.OrderBy(q => q.Number))
        {
            question.Renumber(number++);
        }
    }

    public QuestionGroupMedia AttachMedia(MediaAsset asset, MediaPurpose purpose, string? altText, DateTime nowUtc)
    {
        ArgumentNullException.ThrowIfNull(asset);
        EnsureDraft();

        var expectedKind = purpose == MediaPurpose.Audio ? MediaKind.Audio : MediaKind.Image;
        if (asset.Kind != expectedKind)
        {
            throw new DomainException(
                "media_kind_mismatch",
                purpose == MediaPurpose.Audio ? "聽力音檔必須是音檔。" : "照片與圖表必須是圖片。",
                "mediaAssetId");
        }

        if (purpose == MediaPurpose.Audio && _media.Any(m => m.Purpose == MediaPurpose.Audio))
        {
            throw new DomainException("audio_already_attached", "每個題組只能有一個聽力音檔。", "mediaAssetId");
        }

        if (_media.Any(m => m.MediaAssetId == asset.Id))
        {
            throw new DomainException("media_already_attached", "此媒體已加入題組。", "mediaAssetId");
        }

        var normalizedAlt = Guard.Optional(altText);
        if (expectedKind == MediaKind.Image && normalizedAlt is null)
        {
            throw new DomainException("alt_text_required", "請為圖片填寫替代文字。", "altText");
        }

        if (normalizedAlt is { Length: > QuestionBankRules.AltTextMaxLength })
        {
            throw new DomainException(
                "alt_text_too_long",
                $"替代文字不可超過 {QuestionBankRules.AltTextMaxLength} 字。",
                "altText");
        }

        var media = QuestionGroupMedia.Create(Id, asset.Id, purpose, _media.Count + 1, normalizedAlt, nowUtc);
        _media.Add(media);
        return media;
    }

    public void DetachMedia(Guid mediaId)
    {
        EnsureDraft();
        var media = _media.FirstOrDefault(m => m.Id == mediaId)
            ?? throw new DomainException("media_not_found", "找不到此媒體。", "mediaId");
        _media.Remove(media);
        var order = 1;
        foreach (var item in _media.OrderBy(m => m.SortOrder))
        {
            item.SetSortOrder(order++);
        }
    }

    /// <summary>依題型檢查是否可以發布；回傳所有問題，空清單表示可以發布。</summary>
    public IReadOnlyList<ContentValidationError> Validate(QuestionType questionType)
    {
        ArgumentNullException.ThrowIfNull(questionType);
        var errors = new List<ContentValidationError>();

        if (_questions.Count < questionType.MinQuestions || _questions.Count > questionType.MaxQuestions)
        {
            errors.Add(new(
                "invalid_question_count",
                questionType.MinQuestions == questionType.MaxQuestions
                    ? $"{questionType.Name} 每個題組須有 {questionType.MinQuestions} 題。"
                    : $"{questionType.Name} 每個題組須有 {questionType.MinQuestions} 到 {questionType.MaxQuestions} 題。"));
        }

        if (_passages.Count < questionType.MinPassages || _passages.Count > questionType.MaxPassages)
        {
            errors.Add(new(
                "invalid_passage_count",
                questionType.MaxPassages == 0
                    ? $"{questionType.Name} 不使用文章。"
                    : questionType.MinPassages == questionType.MaxPassages
                        ? $"{questionType.Name} 須有 {questionType.MinPassages} 篇文章。"
                        : $"{questionType.Name} 須有 {questionType.MinPassages} 到 {questionType.MaxPassages} 篇文章。"));
        }

        var audioCount = _media.Count(m => m.Purpose == MediaPurpose.Audio);
        if (questionType.RequiresAudio && audioCount != 1)
        {
            errors.Add(new("audio_required", "聽力題組須有一個音檔。"));
        }

        if (!questionType.RequiresAudio && audioCount > 0)
        {
            errors.Add(new("audio_not_allowed", "閱讀題組不使用音檔。"));
        }

        if (questionType.RequiresAudio && Transcript is null)
        {
            errors.Add(new("transcript_required", "聽力題組須有腳本。"));
        }

        var photoCount = _media.Count(m => m.Purpose == MediaPurpose.Photo);
        if (questionType.RequiresPhoto && photoCount != 1)
        {
            errors.Add(new("photo_required", "Part 1 題組須有一張照片。"));
        }

        if (!questionType.RequiresPhoto && photoCount > 0)
        {
            errors.Add(new("photo_not_allowed", $"{questionType.Name} 不使用照片。"));
        }

        if (!questionType.AllowsGraphic && _media.Any(m => m.Purpose == MediaPurpose.Graphic))
        {
            errors.Add(new("graphic_not_allowed", $"{questionType.Name} 不使用圖表。"));
        }

        if (_media.Any(m => m.Purpose != MediaPurpose.Audio && m.AltText is null))
        {
            errors.Add(new("alt_text_required", "圖片須有替代文字。"));
        }

        foreach (var question in _questions.OrderBy(q => q.Number))
        {
            errors.AddRange(question.Validate(questionType));
        }

        return errors;
    }

    /// <summary>以固定順序串接所有內容後計算 SHA-256，作為版本比較用的指紋。</summary>
    public string ComputeContentHash()
    {
        var builder = new StringBuilder();
        builder.Append("transcript:").Append(Transcript).Append('\n');
        builder.Append("explanation:").Append(Explanation).Append('\n');
        foreach (var passage in _passages.OrderBy(p => p.SortOrder))
        {
            builder.Append("passage:").Append(passage.SortOrder).Append('|').Append(passage.Title).Append('|').Append(passage.Body).Append('\n');
        }

        foreach (var media in _media.OrderBy(m => m.SortOrder))
        {
            builder.Append("media:").Append(media.Purpose).Append('|').Append(media.MediaAssetId).Append('|').Append(media.AltText).Append('\n');
        }

        foreach (var question in _questions.OrderBy(q => q.Number))
        {
            builder.Append("question:").Append(question.Number).Append('|').Append(question.Stem).Append('|')
                .Append(question.SkillId).Append('|').Append(question.IntendedDifficulty).Append('|').Append(question.Explanation).Append('\n');
            foreach (var option in question.Options.OrderBy(o => o.Label, StringComparer.Ordinal))
            {
                builder.Append("option:").Append(option.Label).Append('|').Append(option.Text).Append('|')
                    .Append(option.IsCorrect ? '1' : '0').Append('\n');
            }

            foreach (var topicId in question.Topics.Select(t => t.TopicId).Order())
            {
                builder.Append("topic:").Append(topicId.ToString("N", CultureInfo.InvariantCulture)).Append('\n');
            }
        }

        return Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(builder.ToString())));
    }

    internal void MarkPublished(Guid userId, DateTime nowUtc)
    {
        Guard.Utc(nowUtc, nameof(nowUtc));
        Guard.NotEmpty(userId, nameof(userId));
        EnsureDraft();

        Status = ContentStatus.Published;
        ContentHash = ComputeContentHash();
        PublishedAtUtc = nowUtc;
        PublishedByUserId = userId;
    }

    internal void MarkRetired(Guid userId, DateTime nowUtc)
    {
        Guard.Utc(nowUtc, nameof(nowUtc));
        Guard.NotEmpty(userId, nameof(userId));
        if (Status != ContentStatus.Published)
        {
            throw new DomainException("version_not_published", "只有已發布的版本可以停用。");
        }

        Status = ContentStatus.Retired;
        RetiredAtUtc = nowUtc;
        RetiredByUserId = userId;
    }

    /// <summary>複製內容成新的草稿版本（媒體沿用同一個檔案）。</summary>
    internal QuestionGroupVersion CopyAsDraft(int versionNumber, Guid userId, DateTime nowUtc)
    {
        var draft = CreateDraft(QuestionGroupId, versionNumber, userId, nowUtc);
        draft.Transcript = Transcript;
        draft.Explanation = Explanation;
        draft.Source = Source;
        draft.AuthorNote = AuthorNote;

        foreach (var passage in _passages.OrderBy(p => p.SortOrder))
        {
            draft._passages.Add(QuestionGroupPassage.Create(draft.Id, passage.SortOrder, passage.Title, passage.Body, nowUtc));
        }

        foreach (var question in _questions.OrderBy(q => q.Number))
        {
            draft._questions.Add(Question.Create(draft.Id, question.Number, question.ToInput(), nowUtc));
        }

        foreach (var media in _media.OrderBy(m => m.SortOrder))
        {
            draft._media.Add(QuestionGroupMedia.Create(
                draft.Id,
                media.MediaAssetId,
                media.Purpose,
                media.SortOrder,
                media.AltText,
                nowUtc));
        }

        return draft;
    }

    private static (string? Title, string Body) NormalizePassage(string? title, string body)
    {
        var normalizedTitle = Guard.Optional(title);
        var normalizedBody = Guard.Optional(body)
            ?? throw new DomainException("passage_body_required", "文章內容不可空白。", "body");

        if (normalizedTitle is { Length: > QuestionBankRules.PassageTitleMaxLength })
        {
            throw new DomainException(
                "passage_title_too_long",
                $"文章標題不可超過 {QuestionBankRules.PassageTitleMaxLength} 字。",
                "title");
        }

        if (normalizedBody.Length > QuestionBankRules.LongTextMaxLength)
        {
            throw new DomainException("passage_too_long", "文章過長。", "body");
        }

        return (normalizedTitle, normalizedBody);
    }

    private void EnsureDraft()
    {
        if (Status != ContentStatus.Draft)
        {
            throw new DomainException("version_not_editable", "已發布或停用的版本不可修改，請建立新版本。");
        }
    }

    private QuestionGroupPassage FindPassage(Guid passageId) =>
        _passages.FirstOrDefault(p => p.Id == passageId)
            ?? throw new DomainException("passage_not_found", "找不到此文章。", "passageId");

    private Question FindQuestion(Guid questionId) =>
        _questions.FirstOrDefault(q => q.Id == questionId)
            ?? throw new DomainException("question_not_found", "找不到此題目。", "questionId");
}
