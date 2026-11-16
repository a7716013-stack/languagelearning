using LanguageLearning.Domain.Attempts;
using LanguageLearning.Domain.Exams;
using LanguageLearning.Domain.QuestionBank;
using LanguageLearning.Infrastructure.Persistence;

namespace LanguageLearning.UnitTests;

/// <summary>題庫單元測試共用的資料（題型使用與 Migration 相同的參考資料）。</summary>
internal static class QuestionBankTestData
{
    public static readonly DateTime NowUtc = new(2026, 11, 16, 1, 0, 0, DateTimeKind.Utc);
    public static readonly Guid AuthorId = new("11111111-1111-4111-8111-111111111111");
    public static readonly Guid StudentId = new("22222222-2222-4222-8222-222222222222");

    public static Guid SkillId => QuestionBankReferenceData.Skills[0].Id;

    public static Guid TopicId => QuestionBankReferenceData.Topics[0].Id;

    public static QuestionType TypeOf(string code) =>
        QuestionBankReferenceData.QuestionTypes.Single(t => t.Code == code);

    public static QuestionInput Input(int optionCount = 4, int correctIndex = 0, string? stem = "The report was ------- on time.") =>
        new(
            stem,
            SkillId,
            Difficulty.Medium,
            "解析：依文意選擇正確答案。",
            Enumerable.Range(0, optionCount).Select(i => $"option {(char)('a' + i)}").ToList(),
            correctIndex,
            [TopicId]);

    public static MediaAsset Audio(int durationMs = 30_000, string hashSeed = "a") =>
        MediaAsset.Create(
            MediaKind.Audio,
            "audio/mpeg",
            100_000,
            new string(hashSeed[0], 64),
            $"audio/{Guid.NewGuid():N}",
            "track.mp3",
            null,
            null,
            durationMs,
            MediaSourceType.Original,
            "原創錄音",
            "專案自有",
            AuthorId,
            NowUtc);

    public static MediaAsset Image(string hashSeed = "b") =>
        MediaAsset.Create(
            MediaKind.Image,
            "image/jpeg",
            200_000,
            new string(hashSeed[0], 64),
            $"images/{Guid.NewGuid():N}",
            "photo.jpg",
            1200,
            800,
            null,
            MediaSourceType.OwnPhoto,
            "開發者自行拍攝",
            "專案自有",
            AuthorId,
            NowUtc);

    /// <summary>建立一個已發布的 Part 5 題組（正確答案為 A）。</summary>
    public static QuestionGroup PublishedPart5(string code = "P5-0001")
    {
        var type = TypeOf(QuestionTypeCodes.IncompleteSentences);
        var group = QuestionGroup.Create(code, type, AuthorId, NowUtc);
        group.Draft!.AddQuestion(Input(), NowUtc);
        group.Publish(type, AuthorId, NowUtc);
        return group;
    }

    /// <summary>建立一個已發布的 Part 6 題組（4 題、1 篇文章）。</summary>
    public static QuestionGroup PublishedPart6(string code)
    {
        var type = TypeOf(QuestionTypeCodes.TextCompletion);
        var group = QuestionGroup.Create(code, type, AuthorId, NowUtc);
        var draft = group.Draft!;
        draft.AddPassage("E-mail", "Dear team, ... [131] ... [132] ... [133] ... [134]", NowUtc);
        for (var i = 0; i < 4; i++)
        {
            draft.AddQuestion(Input(stem: null), NowUtc);
        }

        group.Publish(type, AuthorId, NowUtc);
        return group;
    }

    /// <summary>建立一個已發布的 Part 7 題組。</summary>
    public static QuestionGroup PublishedPart7(string code, bool multiple, int questions, int passages)
    {
        var type = TypeOf(multiple ? QuestionTypeCodes.MultiplePassages : QuestionTypeCodes.SinglePassage);
        var group = QuestionGroup.Create(code, type, AuthorId, NowUtc);
        var draft = group.Draft!;
        for (var p = 0; p < passages; p++)
        {
            draft.AddPassage($"Document {p + 1}", $"Passage body {p + 1}.", NowUtc);
        }

        for (var i = 0; i < questions; i++)
        {
            draft.AddQuestion(Input(stem: $"Question {i + 1}?"), NowUtc);
        }

        group.Publish(type, AuthorId, NowUtc);
        return group;
    }

    /// <summary>把題組版本轉成作答資料。</summary>
    public static IEnumerable<AttemptQuestionInfo> ToAttemptQuestions(QuestionGroupVersion version, QuestionType type) =>
        version.Questions.OrderBy(q => q.Number).Select(q => new AttemptQuestionInfo(
            version.QuestionGroupId,
            version.Id,
            q.Id,
            type.Part,
            type.Section,
            q.CorrectOption!.Id));

    /// <summary>建立一個已發布的聽力題組（Part 1–4，題數取題型下限）。</summary>
    public static QuestionGroup PublishedListening(string code, string typeCode)
    {
        var type = TypeOf(typeCode);
        var group = QuestionGroup.Create(code, type, AuthorId, NowUtc);
        var draft = group.Draft!;
        for (var i = 0; i < type.MinQuestions; i++)
        {
            draft.AddQuestion(Input(optionCount: type.OptionCount, stem: type.RequiresStem ? $"Question {i + 1}?" : null), NowUtc);
        }

        draft.AttachMedia(Audio(), MediaPurpose.Audio, null, NowUtc);
        if (type.RequiresPhoto)
        {
            draft.AttachMedia(Image(), MediaPurpose.Photo, "辦公室裡的照片", NowUtc);
        }

        draft.UpdateContent("Transcript", null, null);
        group.Publish(type, AuthorId, NowUtc);
        return group;
    }

    /// <summary>建立一份已發布的閱讀測驗試卷（Part 5 30 題、Part 6 16 題、Part 7 單篇 29 題、多篇 25 題）。</summary>
    public static (ExamPaper Paper, List<AttemptQuestionInfo> Questions) PublishedReadingPaper() =>
        PublishedPaper(ExamPaperKind.ReadingOnly, null);

    /// <summary>建立一份已發布的完整模擬考試卷（200 題）。</summary>
    public static (ExamPaper Paper, List<AttemptQuestionInfo> Questions) PublishedFullPaper(int listeningDurationMs = 45 * 60 * 1000) =>
        PublishedPaper(ExamPaperKind.FullTest, listeningDurationMs);

    private static (ExamPaper Paper, List<AttemptQuestionInfo> Questions) PublishedPaper(ExamPaperKind kind, int? listeningDurationMs)
    {
        var paper = ExamPaper.Create(
            kind == ExamPaperKind.FullTest ? "FULL-01" : "READING-01",
            kind == ExamPaperKind.FullTest ? "模擬考 01" : "閱讀測驗 01",
            kind,
            SeedData.ToeicLrVersionId,
            QuestionBankReferenceData.ToeicRawScoringRuleId,
            AuthorId,
            NowUtc);
        var questions = new List<AttemptQuestionInfo>();

        void Add(QuestionGroup group)
        {
            var version = group.PublishedVersion!;
            var type = QuestionBankReferenceData.QuestionTypes.Single(t => t.Id == group.QuestionTypeId);
            paper.AddGroup(version, type, NowUtc);
            questions.AddRange(ToAttemptQuestions(version, type));
        }

        if (kind == ExamPaperKind.FullTest)
        {
            for (var i = 0; i < 6; i++)
            {
                Add(PublishedListening($"P1-{i:D4}", QuestionTypeCodes.Photographs));
            }

            for (var i = 0; i < 25; i++)
            {
                Add(PublishedListening($"P2-{i:D4}", QuestionTypeCodes.QuestionResponse));
            }

            for (var i = 0; i < 13; i++)
            {
                Add(PublishedListening($"P3-{i:D4}", QuestionTypeCodes.Conversations));
            }

            for (var i = 0; i < 10; i++)
            {
                Add(PublishedListening($"P4-{i:D4}", QuestionTypeCodes.Talks));
            }
        }

        for (var i = 0; i < 30; i++)
        {
            Add(PublishedPart5($"P5-{i:D4}"));
        }

        for (var i = 0; i < 4; i++)
        {
            Add(PublishedPart6($"P6-{i:D4}"));
        }

        // 單篇 29 題：7 組 3 題 + 2 組 4 題；多篇 25 題：5 組 5 題（2 組雙篇、3 組三篇）。
        for (var i = 0; i < 7; i++)
        {
            Add(PublishedPart7($"P7S-{i:D4}", multiple: false, questions: 3, passages: 1));
        }

        for (var i = 0; i < 2; i++)
        {
            Add(PublishedPart7($"P7S-L{i:D3}", multiple: false, questions: 4, passages: 1));
        }

        for (var i = 0; i < 5; i++)
        {
            Add(PublishedPart7($"P7M-{i:D4}", multiple: true, questions: 5, passages: i < 2 ? 2 : 3));
        }

        paper.Publish(QuestionBankReferenceData.QuestionTypes, listeningDurationMs, AuthorId, NowUtc);
        return (paper, OrderLikePaper(paper, questions));
    }

    private static List<AttemptQuestionInfo> OrderLikePaper(ExamPaper paper, List<AttemptQuestionInfo> questions)
    {
        var order = paper.Items.ToDictionary(i => i.GroupVersionId, i => i.SortOrder);
        return questions.OrderBy(q => order[q.GroupVersionId]).ToList();
    }
}
