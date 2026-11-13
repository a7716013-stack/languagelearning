using LanguageLearning.Domain.Exams;

namespace LanguageLearning.UnitTests.Domain;

public sealed class ExamTypeTests
{
    private static readonly ExamType Toeic = TestData.Toeic();

    [Theory]
    [InlineData(10)]
    [InlineData(15)]
    [InlineData(500)]
    [InlineData(985)]
    [InlineData(990)]
    public void IsValidScore_accepts_scores_in_range_and_on_step(int score)
    {
        Assert.True(Toeic.IsValidScore(score));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(5)]
    [InlineData(12)]
    [InlineData(991)]
    [InlineData(995)]
    public void IsValidScore_rejects_scores_out_of_range_or_off_step(int score)
    {
        Assert.False(Toeic.IsValidScore(score));
    }

    [Fact]
    public void IsValidScore_supports_half_band_steps()
    {
        var ielts = new ExamType { Code = "IELTS", MinScore = 0m, MaxScore = 9m, ScoreStep = 0.5m };

        Assert.True(ielts.IsValidScore(6.5m));
        Assert.False(ielts.IsValidScore(6.25m));
    }
}
