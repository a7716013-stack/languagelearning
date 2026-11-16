using LanguageLearning.Domain.Exams;
using LanguageLearning.Domain.QuestionBank;

namespace LanguageLearning.Infrastructure.Persistence;

/// <summary>
/// Phase 2 題庫的參考資料（以 <c>HasData</c> 隨 Migration 建立，固定 Id）。
/// 題型題數依 IIBC 測驗格式頁與 ETS 2018 題型更新說明查證（2026-11-16）。
/// </summary>
public static class QuestionBankReferenceData
{
    public static readonly Guid PhotographsTypeId = new("7e4b2a10-5c3d-4f1e-9a2b-3c4d5e6f0101");
    public static readonly Guid QuestionResponseTypeId = new("7e4b2a10-5c3d-4f1e-9a2b-3c4d5e6f0102");
    public static readonly Guid ConversationsTypeId = new("7e4b2a10-5c3d-4f1e-9a2b-3c4d5e6f0103");
    public static readonly Guid TalksTypeId = new("7e4b2a10-5c3d-4f1e-9a2b-3c4d5e6f0104");
    public static readonly Guid IncompleteSentencesTypeId = new("7e4b2a10-5c3d-4f1e-9a2b-3c4d5e6f0105");
    public static readonly Guid TextCompletionTypeId = new("7e4b2a10-5c3d-4f1e-9a2b-3c4d5e6f0106");
    public static readonly Guid SinglePassageTypeId = new("7e4b2a10-5c3d-4f1e-9a2b-3c4d5e6f0107");
    public static readonly Guid MultiplePassagesTypeId = new("7e4b2a10-5c3d-4f1e-9a2b-3c4d5e6f0108");

    public static readonly Guid ToeicRawScoringRuleId = new("a1f7e5d4-8f6a-4c4b-9d5e-6f7a8b9c0401");

    public static IReadOnlyList<QuestionType> QuestionTypes { get; } =
    [
        CreateType(PhotographsTypeId, QuestionTypeCodes.Photographs, "Part 1 照片描述", 1, ExamSection.Listening, 1, 1, 4, 0, 0,
            requiresAudio: true, requiresPhoto: true, allowsGraphic: false, requiresStem: false, showsOptionText: false),
        CreateType(QuestionResponseTypeId, QuestionTypeCodes.QuestionResponse, "Part 2 應答問題", 2, ExamSection.Listening, 1, 1, 3, 0, 0,
            requiresAudio: true, requiresPhoto: false, allowsGraphic: false, requiresStem: false, showsOptionText: false),
        CreateType(ConversationsTypeId, QuestionTypeCodes.Conversations, "Part 3 簡短對話", 3, ExamSection.Listening, 3, 3, 4, 0, 0,
            requiresAudio: true, requiresPhoto: false, allowsGraphic: true, requiresStem: true, showsOptionText: true),
        CreateType(TalksTypeId, QuestionTypeCodes.Talks, "Part 4 簡短獨白", 4, ExamSection.Listening, 3, 3, 4, 0, 0,
            requiresAudio: true, requiresPhoto: false, allowsGraphic: true, requiresStem: true, showsOptionText: true),
        CreateType(IncompleteSentencesTypeId, QuestionTypeCodes.IncompleteSentences, "Part 5 句子填空", 5, ExamSection.Reading, 1, 1, 4, 0, 0,
            requiresAudio: false, requiresPhoto: false, allowsGraphic: false, requiresStem: true, showsOptionText: true),
        CreateType(TextCompletionTypeId, QuestionTypeCodes.TextCompletion, "Part 6 段落填空", 6, ExamSection.Reading, 4, 4, 4, 1, 1,
            requiresAudio: false, requiresPhoto: false, allowsGraphic: false, requiresStem: false, showsOptionText: true),
        CreateType(SinglePassageTypeId, QuestionTypeCodes.SinglePassage, "Part 7 單篇閱讀", 7, ExamSection.Reading, 2, 4, 4, 1, 1,
            requiresAudio: false, requiresPhoto: false, allowsGraphic: true, requiresStem: true, showsOptionText: true),
        CreateType(MultiplePassagesTypeId, QuestionTypeCodes.MultiplePassages, "Part 7 多篇閱讀", 7, ExamSection.Reading, 5, 5, 4, 2, 3,
            requiresAudio: false, requiresPhoto: false, allowsGraphic: true, requiresStem: true, showsOptionText: true),
    ];

    public static IReadOnlyList<Skill> Skills { get; } =
    [
        CreateSkill(1, "PHOTO_DESCRIPTION", "照片描述", ExamSection.Listening),
        CreateSkill(2, "RESPONSE", "應答判斷", ExamSection.Listening),
        CreateSkill(3, "SPEAKER_INTENT", "說話者意圖", ExamSection.Listening),
        CreateSkill(4, "MAIN_IDEA", "主旨與目的", null),
        CreateSkill(5, "DETAIL", "細節理解", null),
        CreateSkill(6, "INFERENCE", "推論", null),
        CreateSkill(7, "GRAPHIC", "圖表整合", null),
        CreateSkill(8, "PARAPHRASE", "同義改寫", null),
        CreateSkill(9, "VOCABULARY", "字彙", null),
        CreateSkill(10, "GRAMMAR", "文法", ExamSection.Reading),
        CreateSkill(11, "SENTENCE_INSERTION", "句子插入", ExamSection.Reading),
        CreateSkill(12, "MULTI_PASSAGE", "多篇整合", ExamSection.Reading),
    ];

    public static IReadOnlyList<Topic> Topics { get; } =
    [
        CreateTopic(1, "OFFICE", "辦公室"),
        CreateTopic(2, "PERSONNEL", "人事與招募"),
        CreateTopic(3, "MARKETING", "行銷與銷售"),
        CreateTopic(4, "FINANCE", "財務與會計"),
        CreateTopic(5, "PURCHASING", "採購與訂單"),
        CreateTopic(6, "MANUFACTURING", "製造與品管"),
        CreateTopic(7, "CUSTOMER_SERVICE", "客戶服務"),
        CreateTopic(8, "TECHNOLOGY", "科技與設備"),
        CreateTopic(9, "TRAVEL", "旅遊與出差"),
        CreateTopic(10, "TRANSPORTATION", "交通"),
        CreateTopic(11, "DINING", "餐飲"),
        CreateTopic(12, "LODGING", "住宿與房地產"),
        CreateTopic(13, "EVENTS", "會議與活動"),
        CreateTopic(14, "HEALTH", "健康與醫療"),
        CreateTopic(15, "ENTERTAINMENT", "娛樂與文化"),
    ];

    public static IReadOnlyList<ScoringRule> ScoringRules { get; } =
    [
        new ScoringRule
        {
            Id = ToeicRawScoringRuleId,
            ExamVersionId = SeedData.ToeicLrVersionId,
            Code = "TOEIC-LR-RAW",
            Version = 1,
            Kind = ScoringRuleKind.RawCount,
            ParametersJson = null,
            Description = "答對題數與答對率（總分、聽力、閱讀、各 Part）。不換算官方分數。",
            IsDefault = true,
        },
    ];

    private static QuestionType CreateType(
        Guid id,
        string code,
        string name,
        int part,
        ExamSection section,
        int minQuestions,
        int maxQuestions,
        int optionCount,
        int minPassages,
        int maxPassages,
        bool requiresAudio,
        bool requiresPhoto,
        bool allowsGraphic,
        bool requiresStem,
        bool showsOptionText) =>
        new()
        {
            Id = id,
            ExamVersionId = SeedData.ToeicLrVersionId,
            Code = code,
            Name = name,
            Part = part,
            Section = section,
            MinQuestions = minQuestions,
            MaxQuestions = maxQuestions,
            OptionCount = optionCount,
            MinPassages = minPassages,
            MaxPassages = maxPassages,
            RequiresAudio = requiresAudio,
            RequiresPhoto = requiresPhoto,
            AllowsGraphic = allowsGraphic,
            RequiresStem = requiresStem,
            ShowsOptionText = showsOptionText,
            SortOrder = part * 10 + (code == QuestionTypeCodes.MultiplePassages ? 1 : 0),
        };

    private static Skill CreateSkill(int number, string code, string name, ExamSection? section) =>
        new()
        {
            Id = new Guid($"8d5c3b21-6d4e-4a2f-8b3c-4d5e6f7a{number:D4}"),
            Code = code,
            Name = name,
            Section = section,
            SortOrder = number,
        };

    private static Topic CreateTopic(int number, string code, string name) =>
        new()
        {
            Id = new Guid($"9e6d4c32-7e5f-4b3a-9c4d-5e6f7a8b{number:D4}"),
            Code = code,
            Name = name,
            SortOrder = number,
        };
}
