using LanguageLearning.Domain.Exams;

namespace LanguageLearning.UnitTests;

internal static class TestData
{
    public static ExamType Toeic() => new()
    {
        Id = new Guid("5a3e8c21-7b4d-4e6f-8a1b-2c3d4e5f0001"),
        Code = ExamTypeCodes.Toeic,
        Name = "TOEIC Listening & Reading",
        IsAvailable = true,
        MinScore = 10m,
        MaxScore = 990m,
        ScoreStep = 5m,
        SortOrder = 1,
    };
}
