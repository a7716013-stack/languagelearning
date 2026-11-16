using LanguageLearning.Domain.Common;
using LanguageLearning.Domain.QuestionBank;
using static LanguageLearning.UnitTests.QuestionBankTestData;

namespace LanguageLearning.UnitTests.Domain;

public sealed class QuestionGroupTests
{
    [Fact]
    public void Create_starts_with_draft_version_one()
    {
        var group = QuestionGroup.Create(" p5-0001 ", TypeOf(QuestionTypeCodes.IncompleteSentences), AuthorId, NowUtc);

        Assert.Equal("P5-0001", group.Code);
        var draft = Assert.Single(group.Versions);
        Assert.Equal(1, draft.VersionNumber);
        Assert.Equal(ContentStatus.Draft, draft.Status);
        Assert.Same(draft, group.Draft);
        Assert.Null(group.PublishedVersion);
    }

    [Theory]
    [InlineData("")]
    [InlineData("P5 0001")]
    [InlineData("-P5")]
    [InlineData("P5-000000000000000001")]
    public void Create_rejects_invalid_code(string code)
    {
        var ex = Assert.Throws<DomainException>(() =>
            QuestionGroup.Create(code, TypeOf(QuestionTypeCodes.IncompleteSentences), AuthorId, NowUtc));

        Assert.Equal("invalid_group_code", ex.Code);
    }

    [Fact]
    public void Publish_freezes_content_and_records_hash()
    {
        var group = PublishedPart5();
        var published = Assert.Single(group.Versions);

        Assert.Equal(ContentStatus.Published, published.Status);
        Assert.Equal(NowUtc, published.PublishedAtUtc);
        Assert.Equal(AuthorId, published.PublishedByUserId);
        Assert.Matches("^[0-9a-f]{64}$", published.ContentHash!);
        Assert.Null(group.Draft);

        var question = Assert.Single(published.Questions);
        Assert.Equal("A", question.CorrectOption!.Label);
        Assert.Equal(new[] { "A", "B", "C", "D" }, question.Options.Select(o => o.Label).Order().ToArray());

        var ex = Assert.Throws<DomainException>(() => published.UpdateQuestion(question.Id, Input(correctIndex: 1), NowUtc));
        Assert.Equal("version_not_editable", ex.Code);
        Assert.Throws<DomainException>(() => published.AddQuestion(Input(), NowUtc));
        Assert.Throws<DomainException>(() => published.UpdateContent(null, "改寫", null));
    }

    [Fact]
    public void Part2_requires_three_options_audio_and_transcript()
    {
        var type = TypeOf(QuestionTypeCodes.QuestionResponse);
        var group = QuestionGroup.Create("P2-0001", type, AuthorId, NowUtc);
        var draft = group.Draft!;
        draft.AddQuestion(Input(optionCount: 4, stem: null), NowUtc);

        var errors = draft.Validate(type).Select(e => e.Code).ToList();
        Assert.Contains("invalid_option_count", errors);
        Assert.Contains("audio_required", errors);
        Assert.Contains("transcript_required", errors);

        draft.UpdateQuestion(draft.Questions.Single().Id, Input(optionCount: 3, stem: null), NowUtc);
        draft.AttachMedia(Audio(), MediaPurpose.Audio, null, NowUtc);
        draft.UpdateContent("W: Where is the meeting? M: (A) ... (B) ... (C) ...", null, null);

        Assert.Empty(draft.Validate(type));
        group.Publish(type, AuthorId, NowUtc);
        Assert.Equal(3, group.PublishedVersion!.Questions.Single().Options.Count);
    }

    [Fact]
    public void Part1_requires_photo_with_alt_text()
    {
        var type = TypeOf(QuestionTypeCodes.Photographs);
        var group = QuestionGroup.Create("P1-0001", type, AuthorId, NowUtc);
        var draft = group.Draft!;
        draft.AddQuestion(Input(stem: null), NowUtc);
        draft.AttachMedia(Audio(), MediaPurpose.Audio, null, NowUtc);
        draft.UpdateContent("(A) A man is ... (B) ... (C) ... (D) ...", null, null);

        Assert.Contains(draft.Validate(type), e => e.Code == "photo_required");

        var ex = Assert.Throws<DomainException>(() => draft.AttachMedia(Image(), MediaPurpose.Photo, " ", NowUtc));
        Assert.Equal("alt_text_required", ex.Code);

        draft.AttachMedia(Image(), MediaPurpose.Photo, "一位男士在會議室整理文件", NowUtc);
        Assert.Empty(draft.Validate(type));
    }

    [Fact]
    public void Media_kind_must_match_purpose_and_only_one_audio()
    {
        var group = QuestionGroup.Create("P3-0001", TypeOf(QuestionTypeCodes.Conversations), AuthorId, NowUtc);
        var draft = group.Draft!;

        Assert.Equal("media_kind_mismatch", Assert.Throws<DomainException>(() =>
            draft.AttachMedia(Image(), MediaPurpose.Audio, null, NowUtc)).Code);

        draft.AttachMedia(Audio(), MediaPurpose.Audio, null, NowUtc);
        Assert.Equal("audio_already_attached", Assert.Throws<DomainException>(() =>
            draft.AttachMedia(Audio(hashSeed: "c"), MediaPurpose.Audio, null, NowUtc)).Code);
    }

    [Fact]
    public void Reading_types_reject_audio_and_photo()
    {
        var type = TypeOf(QuestionTypeCodes.IncompleteSentences);
        var group = QuestionGroup.Create("P5-0002", type, AuthorId, NowUtc);
        var draft = group.Draft!;
        draft.AddQuestion(Input(), NowUtc);
        draft.AttachMedia(Audio(), MediaPurpose.Audio, null, NowUtc);
        draft.AttachMedia(Image(), MediaPurpose.Graphic, "價目表", NowUtc);

        var errors = draft.Validate(type).Select(e => e.Code).ToList();
        Assert.Contains("audio_not_allowed", errors);
        Assert.Contains("graphic_not_allowed", errors);
    }

    [Theory]
    [InlineData(1, false)]
    [InlineData(2, true)]
    [InlineData(3, true)]
    [InlineData(4, false)]
    public void Multiple_passages_need_two_or_three_passages(int passages, bool valid)
    {
        var type = TypeOf(QuestionTypeCodes.MultiplePassages);
        var group = QuestionGroup.Create("P7M-0001", type, AuthorId, NowUtc);
        var draft = group.Draft!;
        for (var i = 0; i < Math.Min(passages, 3); i++)
        {
            draft.AddPassage($"Doc {i}", $"Body {i}", NowUtc);
        }

        if (passages > 3)
        {
            Assert.Equal("too_many_passages", Assert.Throws<DomainException>(() =>
                draft.AddPassage("Doc 4", "Body 4", NowUtc)).Code);
            return;
        }

        for (var i = 0; i < 5; i++)
        {
            draft.AddQuestion(Input(stem: $"Q{i}?"), NowUtc);
        }

        Assert.Equal(valid, draft.Validate(type).All(e => e.Code != "invalid_passage_count"));
    }

    [Fact]
    public void Single_passage_allows_two_to_four_questions()
    {
        var type = TypeOf(QuestionTypeCodes.SinglePassage);
        var group = QuestionGroup.Create("P7S-0001", type, AuthorId, NowUtc);
        var draft = group.Draft!;
        draft.AddPassage("Notice", "Body", NowUtc);
        draft.AddQuestion(Input(stem: "Q1?"), NowUtc);

        Assert.Contains(draft.Validate(type), e => e.Code == "invalid_question_count");

        draft.AddQuestion(Input(stem: "Q2?"), NowUtc);
        Assert.Empty(draft.Validate(type));
    }

    [Fact]
    public void Validation_reports_missing_stem_and_explanation()
    {
        var type = TypeOf(QuestionTypeCodes.IncompleteSentences);
        var group = QuestionGroup.Create("P5-0003", type, AuthorId, NowUtc);
        group.Draft!.AddQuestion(Input(stem: "  ") with { Explanation = " " }, NowUtc);

        var errors = group.Draft!.Validate(type).Select(e => e.Code).ToList();
        Assert.Contains("stem_required", errors);
        Assert.Contains("explanation_required", errors);
        Assert.Equal("question_group_invalid", Assert.Throws<DomainException>(() =>
            group.Publish(type, AuthorId, NowUtc)).Code);
    }

    [Theory]
    [InlineData(2)]
    [InlineData(5)]
    public void Question_input_requires_three_or_four_options(int optionCount)
    {
        var group = QuestionGroup.Create("P5-0004", TypeOf(QuestionTypeCodes.IncompleteSentences), AuthorId, NowUtc);

        Assert.Equal("invalid_option_count", Assert.Throws<DomainException>(() =>
            group.Draft!.AddQuestion(Input(optionCount: optionCount), NowUtc)).Code);
    }

    [Fact]
    public void Question_input_rejects_duplicate_options_and_bad_correct_index()
    {
        var draft = QuestionGroup.Create("P5-0005", TypeOf(QuestionTypeCodes.IncompleteSentences), AuthorId, NowUtc).Draft!;

        Assert.Equal("duplicate_options", Assert.Throws<DomainException>(() =>
            draft.AddQuestion(Input() with { Options = ["same", "same", "x", "y"] }, NowUtc)).Code);
        Assert.Equal("invalid_correct_option", Assert.Throws<DomainException>(() =>
            draft.AddQuestion(Input(correctIndex: 4), NowUtc)).Code);
    }

    [Fact]
    public void Update_changes_correct_option_in_place()
    {
        var draft = QuestionGroup.Create("P5-0006", TypeOf(QuestionTypeCodes.IncompleteSentences), AuthorId, NowUtc).Draft!;
        var question = draft.AddQuestion(Input(correctIndex: 0), NowUtc);
        var optionIds = question.Options.Select(o => o.Id).ToHashSet();

        draft.UpdateQuestion(question.Id, Input(correctIndex: 2), NowUtc);

        Assert.Equal("C", question.CorrectOption!.Label);
        Assert.Single(question.Options, o => o.IsCorrect);
        Assert.True(optionIds.SetEquals(question.Options.Select(o => o.Id)));
    }

    [Fact]
    public void Remove_question_renumbers_remaining_questions()
    {
        var draft = QuestionGroup.Create("P7S-0002", TypeOf(QuestionTypeCodes.SinglePassage), AuthorId, NowUtc).Draft!;
        var first = draft.AddQuestion(Input(stem: "Q1?"), NowUtc);
        draft.AddQuestion(Input(stem: "Q2?"), NowUtc);
        draft.AddQuestion(Input(stem: "Q3?"), NowUtc);

        draft.RemoveQuestion(first.Id);

        Assert.Equal(new[] { 1, 2 }, draft.Questions.Select(q => q.Number).Order().ToArray());
        Assert.Equal("Q2?", draft.Questions.Single(q => q.Number == 1).Stem);
    }

    [Fact]
    public void New_version_copies_content_and_publishing_retires_previous()
    {
        var type = TypeOf(QuestionTypeCodes.IncompleteSentences);
        var group = PublishedPart5();
        var v1 = group.PublishedVersion!;
        var later = NowUtc.AddDays(1);

        var v2 = group.CreateDraftFromPublished(AuthorId, later);

        Assert.Equal(2, v2.VersionNumber);
        Assert.NotEqual(v1.Id, v2.Id);
        var copied = Assert.Single(v2.Questions);
        Assert.NotEqual(v1.Questions.Single().Id, copied.Id);
        Assert.Equal(v1.Questions.Single().Stem, copied.Stem);
        Assert.Equal("A", copied.CorrectOption!.Label);
        Assert.Equal("draft_already_exists", Assert.Throws<DomainException>(() =>
            group.CreateDraftFromPublished(AuthorId, later)).Code);

        v2.UpdateQuestion(copied.Id, Input(correctIndex: 1), later);
        group.Publish(type, AuthorId, later);

        Assert.Equal(ContentStatus.Retired, v1.Status);
        Assert.Equal(later, v1.RetiredAtUtc);
        Assert.Same(v2, group.PublishedVersion);
        Assert.Equal(2, group.PublishedVersionNumber);
        Assert.NotEqual(v1.ContentHash, v2.ContentHash);
        Assert.Equal("A", v1.Questions.Single().CorrectOption!.Label);
    }

    [Fact]
    public void Same_content_produces_same_hash()
    {
        var group = PublishedPart5();
        var v2 = group.CreateDraftFromPublished(AuthorId, NowUtc);

        Assert.Equal(group.PublishedVersion!.ContentHash, v2.ComputeContentHash());
    }

    [Fact]
    public void Retire_and_discard_follow_state_rules()
    {
        var group = PublishedPart5();
        Assert.Equal("no_draft_version", Assert.Throws<DomainException>(() => group.DiscardDraft()).Code);

        group.CreateDraftFromPublished(AuthorId, NowUtc);
        group.DiscardDraft();
        Assert.Single(group.Versions);

        Assert.Equal(1, group.PublishedVersionNumber);
        group.Retire(AuthorId, NowUtc);
        Assert.Equal(ContentStatus.Retired, group.Versions.Single().Status);
        Assert.Null(group.PublishedVersionNumber);
        Assert.Equal("no_published_version", Assert.Throws<DomainException>(() => group.Retire(AuthorId, NowUtc)).Code);

        var fresh = QuestionGroup.Create("P5-0009", TypeOf(QuestionTypeCodes.IncompleteSentences), AuthorId, NowUtc);
        Assert.Equal("cannot_discard_only_version", Assert.Throws<DomainException>(() => fresh.DiscardDraft()).Code);
    }

    [Fact]
    public void Publish_rejects_type_of_other_group()
    {
        var group = QuestionGroup.Create("P5-0010", TypeOf(QuestionTypeCodes.IncompleteSentences), AuthorId, NowUtc);

        Assert.Throws<ArgumentException>(() => group.Publish(TypeOf(QuestionTypeCodes.Talks), AuthorId, NowUtc));
    }
}
