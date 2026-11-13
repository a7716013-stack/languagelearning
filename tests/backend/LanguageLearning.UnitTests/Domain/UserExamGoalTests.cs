using LanguageLearning.Domain.Common;
using LanguageLearning.Domain.Exams;
using LanguageLearning.Domain.Goals;

namespace LanguageLearning.UnitTests.Domain;

public sealed class UserExamGoalTests
{
    private static readonly DateOnly Today = new(2026, 11, 5);
    private static readonly DateTime NowUtc = new(2026, 11, 5, 2, 0, 0, DateTimeKind.Utc);
    private static readonly Guid UserId = Guid.NewGuid();

    [Fact]
    public void Create_sets_values_and_timestamps()
    {
        var goal = UserExamGoal.Create(UserId, TestData.Toeic(), 800, Today.AddDays(90), 60, Today, NowUtc);

        Assert.Equal(UserId, goal.UserId);
        Assert.Equal(800m, goal.TargetScore);
        Assert.Equal(Today.AddDays(90), goal.ExamDate);
        Assert.Equal(60, goal.DailyStudyMinutes);
        Assert.Equal(NowUtc, goal.CreatedAtUtc);
        Assert.Equal(NowUtc, goal.UpdatedAtUtc);
        Assert.Equal(90, goal.DaysUntilExam(Today));
        Assert.NotEqual(Guid.Empty, goal.Id);
    }

    [Fact]
    public void Create_allows_exam_today()
    {
        var goal = UserExamGoal.Create(UserId, TestData.Toeic(), 600, Today, 30, Today, NowUtc);

        Assert.Equal(0, goal.DaysUntilExam(Today));
    }

    [Fact]
    public void Create_rejects_unavailable_exam()
    {
        var ielts = new ExamType
        {
            Id = Guid.NewGuid(),
            Code = ExamTypeCodes.Ielts,
            Name = "IELTS",
            IsAvailable = false,
            MinScore = 0,
            MaxScore = 9,
            ScoreStep = 0.5m,
        };

        var ex = Assert.Throws<DomainException>(() =>
            UserExamGoal.Create(UserId, ielts, 7, Today.AddDays(30), 60, Today, NowUtc));

        Assert.Equal("exam_not_available", ex.Code);
        Assert.Equal("examTypeCode", ex.Field);
    }

    [Theory]
    [InlineData(5)]
    [InlineData(803)]
    [InlineData(995)]
    public void Create_rejects_invalid_target_score(int score)
    {
        var ex = Assert.Throws<DomainException>(() =>
            UserExamGoal.Create(UserId, TestData.Toeic(), score, Today.AddDays(30), 60, Today, NowUtc));

        Assert.Equal("invalid_target_score", ex.Code);
        Assert.Equal("targetScore", ex.Field);
    }

    [Fact]
    public void Create_rejects_past_exam_date()
    {
        var ex = Assert.Throws<DomainException>(() =>
            UserExamGoal.Create(UserId, TestData.Toeic(), 700, Today.AddDays(-1), 60, Today, NowUtc));

        Assert.Equal("invalid_exam_date", ex.Code);
    }

    [Fact]
    public void Create_rejects_exam_date_too_far_ahead()
    {
        var ex = Assert.Throws<DomainException>(() =>
            UserExamGoal.Create(
                UserId,
                TestData.Toeic(),
                700,
                Today.AddYears(GoalRules.MaxYearsAhead).AddDays(1),
                60,
                Today,
                NowUtc));

        Assert.Equal("invalid_exam_date", ex.Code);
    }

    [Theory]
    [InlineData(9)]
    [InlineData(601)]
    public void Create_rejects_daily_minutes_out_of_range(int minutes)
    {
        var ex = Assert.Throws<DomainException>(() =>
            UserExamGoal.Create(UserId, TestData.Toeic(), 700, Today.AddDays(10), minutes, Today, NowUtc));

        Assert.Equal("invalid_daily_minutes", ex.Code);
    }

    [Fact]
    public void Create_requires_utc_time()
    {
        var local = DateTime.SpecifyKind(NowUtc, DateTimeKind.Local);

        Assert.Throws<ArgumentException>(() =>
            UserExamGoal.Create(UserId, TestData.Toeic(), 700, Today.AddDays(10), 60, Today, local));
    }

    [Fact]
    public void Update_changes_values_and_updated_time_only()
    {
        var toeic = TestData.Toeic();
        var goal = UserExamGoal.Create(UserId, toeic, 700, Today.AddDays(10), 60, Today, NowUtc);
        var later = NowUtc.AddHours(1);

        goal.Update(toeic, 850, Today.AddDays(20), 90, Today, later);

        Assert.Equal(850m, goal.TargetScore);
        Assert.Equal(Today.AddDays(20), goal.ExamDate);
        Assert.Equal(90, goal.DailyStudyMinutes);
        Assert.Equal(NowUtc, goal.CreatedAtUtc);
        Assert.Equal(later, goal.UpdatedAtUtc);
    }

    [Fact]
    public void Update_rejects_different_exam_type()
    {
        var goal = UserExamGoal.Create(UserId, TestData.Toeic(), 700, Today.AddDays(10), 60, Today, NowUtc);
        var other = TestData.Toeic();
        other.Id = Guid.NewGuid();

        Assert.Throws<InvalidOperationException>(() =>
            goal.Update(other, 700, Today.AddDays(10), 60, Today, NowUtc));
    }
}
