using LanguageLearning.Domain.Common;

namespace LanguageLearning.Domain.QuestionBank;

/// <summary>新增或修改題目時的輸入。選項依序指派代號 A、B、C、D。</summary>
public sealed record QuestionInput(
    string? Stem,
    Guid SkillId,
    Difficulty IntendedDifficulty,
    string Explanation,
    IReadOnlyList<string> Options,
    int CorrectOptionIndex,
    IReadOnlyCollection<Guid> TopicIds);

/// <summary>題組版本內的一題。只能透過所屬的草稿版本修改。</summary>
public sealed class Question
{
    private readonly List<QuestionOption> _options = [];
    private readonly List<QuestionTopic> _topics = [];

    private Question()
    {
    }

    public Guid Id { get; private set; }

    public Guid GroupVersionId { get; private set; }

    /// <summary>題組內的題號，從 1 開始。</summary>
    public int Number { get; private set; }

    public string? Stem { get; private set; }

    public Guid SkillId { get; private set; }

    public Difficulty IntendedDifficulty { get; private set; }

    public string Explanation { get; private set; } = string.Empty;

    public IReadOnlyCollection<QuestionOption> Options => _options;

    public IReadOnlyCollection<QuestionTopic> Topics => _topics;

    public QuestionOption? CorrectOption => _options.FirstOrDefault(o => o.IsCorrect);

    internal static Question Create(Guid groupVersionId, int number, QuestionInput input, DateTime nowUtc)
    {
        var question = new Question
        {
            Id = Guid.CreateVersion7(nowUtc),
            GroupVersionId = groupVersionId,
            Number = number,
        };
        question.Apply(input, nowUtc);
        return question;
    }

    /// <summary>檢查不依賴題型的基本規則（選項數量、正確答案、長度）。題型規則於發布時檢查。</summary>
    internal static void ValidateInput(QuestionInput input)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(input.Options);
        ArgumentNullException.ThrowIfNull(input.TopicIds);
        Guard.NotEmpty(input.SkillId, nameof(input.SkillId));

        if (input.Options.Count is < QuestionBankRules.MinOptions or > QuestionBankRules.MaxOptions)
        {
            throw new DomainException(
                "invalid_option_count",
                $"每題須有 {QuestionBankRules.MinOptions} 到 {QuestionBankRules.MaxOptions} 個選項。",
                "options");
        }

        if (input.CorrectOptionIndex < 0 || input.CorrectOptionIndex >= input.Options.Count)
        {
            throw new DomainException("invalid_correct_option", "請指定一個正確答案。", "correctOptionIndex");
        }

        if (input.Options.Any(o => string.IsNullOrWhiteSpace(o)))
        {
            throw new DomainException("option_text_required", "選項內容不可空白。", "options");
        }

        if (input.Options.Any(o => o.Trim().Length > QuestionBankRules.OptionTextMaxLength))
        {
            throw new DomainException(
                "option_text_too_long",
                $"選項內容不可超過 {QuestionBankRules.OptionTextMaxLength} 字。",
                "options");
        }

        if (input.Options.Select(o => o.Trim()).Distinct(StringComparer.Ordinal).Count() != input.Options.Count)
        {
            throw new DomainException("duplicate_options", "選項內容不可重複。", "options");
        }

        if (Guard.Optional(input.Stem) is { Length: > QuestionBankRules.StemMaxLength })
        {
            throw new DomainException(
                "stem_too_long",
                $"題幹不可超過 {QuestionBankRules.StemMaxLength} 字。",
                "stem");
        }

        if ((input.Explanation ?? string.Empty).Trim().Length > QuestionBankRules.LongTextMaxLength)
        {
            throw new DomainException("explanation_too_long", "解析過長。", "explanation");
        }

        if (input.TopicIds.Any(id => id == Guid.Empty) || input.TopicIds.Distinct().Count() != input.TopicIds.Count)
        {
            throw new DomainException("invalid_topics", "主題不可重複。", "topicIds");
        }
    }

    internal void Apply(QuestionInput input, DateTime nowUtc)
    {
        ValidateInput(input);

        Stem = Guard.Optional(input.Stem);
        SkillId = input.SkillId;
        IntendedDifficulty = input.IntendedDifficulty;
        Explanation = (input.Explanation ?? string.Empty).Trim();

        // 依代號就地更新選項，避免同一代號先刪除再新增。
        for (var i = 0; i < input.Options.Count; i++)
        {
            var label = QuestionBankRules.OptionLabels[i];
            var text = input.Options[i].Trim();
            var isCorrect = i == input.CorrectOptionIndex;
            var existing = _options.FirstOrDefault(o => o.Label == label);
            if (existing is null)
            {
                _options.Add(QuestionOption.Create(Id, label, text, isCorrect, nowUtc));
            }
            else
            {
                existing.Update(text, isCorrect);
            }
        }

        _options.RemoveAll(o => o.Label[0] - 'A' >= input.Options.Count);

        _topics.RemoveAll(t => !input.TopicIds.Contains(t.TopicId));
        foreach (var topicId in input.TopicIds.Where(id => _topics.All(t => t.TopicId != id)))
        {
            _topics.Add(QuestionTopic.Create(Id, topicId));
        }
    }

    internal void Renumber(int number) => Number = number;

    internal QuestionInput ToInput() =>
        new(
            Stem,
            SkillId,
            IntendedDifficulty,
            Explanation,
            _options.OrderBy(o => o.Label, StringComparer.Ordinal).Select(o => o.Text).ToList(),
            _options.OrderBy(o => o.Label, StringComparer.Ordinal).ToList().FindIndex(o => o.IsCorrect),
            _topics.Select(t => t.TopicId).ToList());

    internal IReadOnlyList<ContentValidationError> Validate(QuestionType questionType)
    {
        var errors = new List<ContentValidationError>();
        var prefix = $"第 {Number} 題";

        if (_options.Count != questionType.OptionCount)
        {
            errors.Add(new("invalid_option_count", $"{prefix}須有 {questionType.OptionCount} 個選項。"));
        }

        var correctCount = _options.Count(o => o.IsCorrect);
        if (correctCount != 1)
        {
            errors.Add(new("invalid_correct_option", $"{prefix}必須恰好有一個正確答案。"));
        }

        if (questionType.RequiresStem && Stem is null)
        {
            errors.Add(new("stem_required", $"{prefix}缺少題幹。"));
        }

        if (string.IsNullOrWhiteSpace(Explanation))
        {
            errors.Add(new("explanation_required", $"{prefix}缺少解析。"));
        }

        return errors;
    }
}
