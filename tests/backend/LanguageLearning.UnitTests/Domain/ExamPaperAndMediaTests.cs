using LanguageLearning.Domain.Common;
using LanguageLearning.Domain.Exams;
using LanguageLearning.Domain.QuestionBank;
using LanguageLearning.Infrastructure.Persistence;
using static LanguageLearning.UnitTests.QuestionBankTestData;

namespace LanguageLearning.UnitTests.Domain;

public sealed class ExamPaperAndMediaTests
{
    [Fact]
    public void Reading_paper_with_toeic_distribution_can_be_published()
    {
        var (paper, questions) = PublishedReadingPaper();

        Assert.Equal(ContentStatus.Published, paper.Status);
        Assert.Equal(100, paper.TotalQuestions);
        Assert.Equal(100, questions.Count);
        Assert.Null(paper.ListeningDurationMs);
        Assert.Equal(ToeicBlueprint.ReadingTimeLimitSeconds, paper.ReadingTimeLimitSeconds);
        Assert.Equal(Enumerable.Range(1, paper.Items.Count), paper.Items.Select(i => i.SortOrder).Order());

        var parts = paper.Items.OrderBy(i => i.SortOrder).Select(i => i.Part).ToList();
        Assert.Equal(parts.Order(), parts);
    }

    [Fact]
    public void Paper_rejects_draft_versions_listening_groups_and_duplicates()
    {
        var paper = NewPaper(ExamPaperKind.ReadingOnly);
        var draftGroup = QuestionGroup.Create("P5-0100", TypeOf(QuestionTypeCodes.IncompleteSentences), AuthorId, NowUtc);
        draftGroup.Draft!.AddQuestion(Input(), NowUtc);

        Assert.Equal("version_not_published", Assert.Throws<DomainException>(() =>
            paper.AddGroup(draftGroup.Draft!, TypeOf(QuestionTypeCodes.IncompleteSentences), NowUtc)).Code);

        var listening = PublishedListening("P2-0100", QuestionTypeCodes.QuestionResponse);
        Assert.Equal("listening_not_allowed", Assert.Throws<DomainException>(() =>
            paper.AddGroup(listening.PublishedVersion!, TypeOf(QuestionTypeCodes.QuestionResponse), NowUtc)).Code);

        var part5 = PublishedPart5("P5-0101");
        paper.AddGroup(part5.PublishedVersion!, TypeOf(QuestionTypeCodes.IncompleteSentences), NowUtc);
        Assert.Equal("group_already_in_paper", Assert.Throws<DomainException>(() =>
            paper.AddGroup(part5.PublishedVersion!, TypeOf(QuestionTypeCodes.IncompleteSentences), NowUtc)).Code);
    }

    [Fact]
    public void Paper_keeps_part_order_when_groups_are_added_out_of_order()
    {
        var paper = NewPaper(ExamPaperKind.ReadingOnly);
        var part6 = PublishedPart6("P6-0100");
        var part5 = PublishedPart5("P5-0102");

        paper.AddGroup(part6.PublishedVersion!, TypeOf(QuestionTypeCodes.TextCompletion), NowUtc);
        paper.AddGroup(part5.PublishedVersion!, TypeOf(QuestionTypeCodes.IncompleteSentences), NowUtc);

        var ordered = paper.Items.OrderBy(i => i.SortOrder).ToList();
        Assert.Equal(new[] { 5, 6 }, ordered.Select(i => i.Part).ToArray());
        Assert.Equal(new[] { 1, 4 }, ordered.Select(i => i.QuestionCount).ToArray());
    }

    [Fact]
    public void Publish_reports_wrong_question_counts()
    {
        var paper = NewPaper(ExamPaperKind.ReadingOnly);
        paper.AddGroup(PublishedPart5("P5-0103").PublishedVersion!, TypeOf(QuestionTypeCodes.IncompleteSentences), NowUtc);

        var errors = paper.Validate(QuestionBankReferenceData.QuestionTypes);
        Assert.Equal(4, errors.Count);
        Assert.Contains(errors, e => e.Message.Contains("須有 30 題，目前 1 題", StringComparison.Ordinal));

        Assert.Equal("exam_paper_invalid", Assert.Throws<DomainException>(() =>
            paper.Publish(QuestionBankReferenceData.QuestionTypes, null, AuthorId, NowUtc)).Code);
        Assert.Equal(ContentStatus.Draft, paper.Status);
    }

    [Fact]
    public void Published_paper_cannot_change_and_can_be_retired()
    {
        var (paper, _) = PublishedReadingPaper();

        Assert.Equal("paper_not_editable", Assert.Throws<DomainException>(() =>
            paper.RemoveGroup(paper.Items.First().GroupVersionId)).Code);

        paper.Retire(AuthorId, NowUtc);
        Assert.Equal(ContentStatus.Retired, paper.Status);
        Assert.Equal("paper_not_published", Assert.Throws<DomainException>(() => paper.Retire(AuthorId, NowUtc)).Code);
    }

    [Fact]
    public void Media_asset_validates_type_size_and_source()
    {
        var image = Image();
        Assert.Equal(MediaKind.Image, image.Kind);
        Assert.Null(image.DurationMs);
        Assert.Equal(new string('b', 64), image.Sha256);

        Assert.Equal("unsupported_media_type", Assert.Throws<DomainException>(() => CreateImage(contentType: "image/gif")).Code);
        Assert.Equal("media_too_large", Assert.Throws<DomainException>(() => CreateImage(size: MediaRules.ImageMaxBytes + 1)).Code);
        Assert.Equal("media_source_required", Assert.Throws<DomainException>(() => CreateImage(source: " ")).Code);
        Assert.Equal("media_license_required", Assert.Throws<DomainException>(() => CreateImage(license: "")).Code);
        Assert.Equal("invalid_image_size", Assert.Throws<DomainException>(() => CreateImage(width: 0)).Code);
        Assert.Throws<ArgumentException>(() => CreateImage(sha256: "not-a-hash"));

        Assert.Equal("invalid_audio_duration", Assert.Throws<DomainException>(() => Audio(durationMs: 0)).Code);
        Assert.Equal("photo.jpg", CreateImage(fileName: @"C:\fakepath\photo.jpg").OriginalFileName);
    }

    private static ExamPaper NewPaper(ExamPaperKind kind) =>
        ExamPaper.Create(
            "PAPER-01",
            "測試試卷",
            kind,
            SeedData.ToeicLrVersionId,
            QuestionBankReferenceData.ToeicRawScoringRuleId,
            AuthorId,
            NowUtc);

    private static MediaAsset CreateImage(
        string contentType = "image/png",
        long size = 1000,
        string? sha256 = null,
        string source = "原創圖表",
        string license = "專案自有",
        int width = 100,
        string fileName = "chart.png") =>
        MediaAsset.Create(
            MediaKind.Image,
            contentType,
            size,
            sha256 ?? new string('c', 64),
            "images/test",
            fileName,
            width,
            100,
            null,
            MediaSourceType.Original,
            source,
            license,
            AuthorId,
            NowUtc);
}
