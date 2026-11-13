using System.Net;
using System.Net.Http.Json;
using LanguageLearning.Application.Exams;
using LanguageLearning.Application.Goals;
using LanguageLearning.Application.Users;
using LanguageLearning.IntegrationTests.Infrastructure;

namespace LanguageLearning.IntegrationTests;

public sealed class ProfileAndGoalTests(ApiFixture fixture) : IClassFixture<ApiFixture>
{
    private static readonly DateOnly FutureDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(120));

    private ApiFactory Factory => fixture.Factory;

    [Fact]
    public async Task Exam_types_are_seeded_and_only_toeic_is_available()
    {
        using var client = Factory.CreateTestClient();

        var types = await client.GetJsonAsync<List<ExamTypeDto>>("/api/exam-types");

        Assert.Equal(new[] { "TOEIC", "IELTS", "TOEFL" }, types.Select(x => x.Code));
        Assert.True(types[0].IsAvailable);
        Assert.False(types[1].IsAvailable);
        Assert.False(types[2].IsAvailable);
        Assert.Equal(10m, types[0].MinScore);
        Assert.Equal(990m, types[0].MaxScore);
        Assert.Equal(5m, types[0].ScoreStep);
    }

    [Fact]
    public async Task Exam_versions_can_be_filtered_by_exam_type()
    {
        using var client = Factory.CreateTestClient();

        var toeic = await client.GetJsonAsync<List<ExamVersionDto>>("/api/exam-versions?examType=toeic");
        var ielts = await client.GetJsonAsync<List<ExamVersionDto>>("/api/exam-versions?examType=IELTS");

        var version = Assert.Single(toeic);
        Assert.Equal("TOEIC-LR", version.Code);
        Assert.True(version.IsCurrent);
        Assert.Empty(ielts);
    }

    [Fact]
    public async Task Profile_can_be_updated()
    {
        var (client, _) = await TestUsers.LoggedInAsync(Factory);
        using (client)
        {
            var response = await client.PutAsync(
                "/api/users/me",
                new { displayName = "  新名字  ", timeZoneId = "Asia/Tokyo", uiTheme = "dark" });
            await TestClient.EnsureStatusAsync(response, HttpStatusCode.OK);

            var me = await client.GetJsonAsync<CurrentUserDto>("/api/users/me");
            Assert.Equal("新名字", me.DisplayName);
            Assert.Equal("Asia/Tokyo", me.TimeZoneId);
            Assert.Equal("dark", me.UiTheme);
        }
    }

    [Fact]
    public async Task Profile_update_rejects_invalid_time_zone()
    {
        var (client, _) = await TestUsers.LoggedInAsync(Factory);
        using (client)
        {
            var response = await client.PutAsync(
                "/api/users/me",
                new { displayName = "名字", timeZoneId = "Not/AZone", uiTheme = "light" });

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            var json = await TestClient.ReadJsonAsync(response);
            Assert.True(json.GetProperty("errors").TryGetProperty("timeZoneId", out _));
        }
    }

    [Fact]
    public async Task Onboarding_requires_goal_then_completes()
    {
        var (client, _) = await TestUsers.LoggedInAsync(Factory);
        using (client)
        {
            var tooEarly = await client.PostAsync("/api/users/me/onboarding/complete");
            Assert.Equal(HttpStatusCode.BadRequest, tooEarly.StatusCode);
            Assert.Equal("goal_required", await TestClient.ReadProblemCodeAsync(tooEarly));

            var save = await client.PutAsync(
                "/api/users/me/exam-goals/TOEIC",
                new { targetScore = 850, examDate = FutureDate, dailyStudyMinutes = 60 });
            await TestClient.EnsureStatusAsync(save, HttpStatusCode.OK);
            var goal = (await save.Content.ReadFromJsonAsync<ExamGoalDto>(TestContext.Current.CancellationToken))!;
            Assert.Equal("TOEIC", goal.ExamTypeCode);
            Assert.Equal(850m, goal.TargetScore);
            Assert.False(string.IsNullOrEmpty(goal.Version));
            Assert.InRange(goal.DaysUntilExam, 118, 121);

            var complete = await client.PostAsync("/api/users/me/onboarding/complete");
            await TestClient.EnsureStatusAsync(complete, HttpStatusCode.OK);

            var me = await client.GetJsonAsync<CurrentUserDto>("/api/users/me");
            Assert.True(me.OnboardingCompleted);

            var goals = await client.GetJsonAsync<List<ExamGoalDto>>("/api/users/me/exam-goals");
            Assert.Single(goals);
        }
    }

    [Theory]
    [InlineData(803, "invalid_target_score")]
    [InlineData(995, "invalid_target_score")]
    public async Task Goal_rejects_invalid_score(int score, string code)
    {
        var (client, _) = await TestUsers.LoggedInAsync(Factory);
        using (client)
        {
            var response = await client.PutAsync(
                "/api/users/me/exam-goals/TOEIC",
                new { targetScore = score, examDate = FutureDate, dailyStudyMinutes = 60 });

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Equal(code, await TestClient.ReadProblemCodeAsync(response));
        }
    }

    [Fact]
    public async Task Goal_rejects_past_exam_date_and_unavailable_exam()
    {
        var (client, _) = await TestUsers.LoggedInAsync(Factory);
        using (client)
        {
            var past = await client.PutAsync(
                "/api/users/me/exam-goals/TOEIC",
                new { targetScore = 800, examDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-3)), dailyStudyMinutes = 60 });
            Assert.Equal(HttpStatusCode.BadRequest, past.StatusCode);
            Assert.Equal("invalid_exam_date", await TestClient.ReadProblemCodeAsync(past));

            var ielts = await client.PutAsync(
                "/api/users/me/exam-goals/IELTS",
                new { targetScore = 7, examDate = FutureDate, dailyStudyMinutes = 60 });
            Assert.Equal(HttpStatusCode.BadRequest, ielts.StatusCode);
            Assert.Equal("exam_not_available", await TestClient.ReadProblemCodeAsync(ielts));

            var unknown = await client.PutAsync(
                "/api/users/me/exam-goals/GEPT",
                new { targetScore = 7, examDate = FutureDate, dailyStudyMinutes = 60 });
            Assert.Equal(HttpStatusCode.NotFound, unknown.StatusCode);
        }
    }

    [Fact]
    public async Task Goal_update_uses_optimistic_concurrency()
    {
        var (client, _) = await TestUsers.LoggedInAsync(Factory);
        using (client)
        {
            var created = await client.PutAsync(
                "/api/users/me/exam-goals/TOEIC",
                new { targetScore = 700, examDate = FutureDate, dailyStudyMinutes = 30 });
            await TestClient.EnsureStatusAsync(created, HttpStatusCode.OK);
            var version = (await created.Content.ReadFromJsonAsync<ExamGoalDto>(TestContext.Current.CancellationToken))!.Version;

            var first = await client.PutAsync(
                "/api/users/me/exam-goals/TOEIC",
                new { targetScore = 750, examDate = FutureDate, dailyStudyMinutes = 45, version });
            await TestClient.EnsureStatusAsync(first, HttpStatusCode.OK);

            var stale = await client.PutAsync(
                "/api/users/me/exam-goals/TOEIC",
                new { targetScore = 800, examDate = FutureDate, dailyStudyMinutes = 60, version });
            Assert.Equal(HttpStatusCode.Conflict, stale.StatusCode);
            Assert.Equal("goal_conflict", await TestClient.ReadProblemCodeAsync(stale));

            var missingVersion = await client.PutAsync(
                "/api/users/me/exam-goals/TOEIC",
                new { targetScore = 800, examDate = FutureDate, dailyStudyMinutes = 60 });
            Assert.Equal(HttpStatusCode.Conflict, missingVersion.StatusCode);

            var goals = await client.GetJsonAsync<List<ExamGoalDto>>("/api/users/me/exam-goals");
            Assert.Equal(750m, Assert.Single(goals).TargetScore);
        }
    }

    [Fact]
    public async Task Users_only_see_their_own_goals()
    {
        var (alice, _) = await TestUsers.LoggedInAsync(Factory);
        var (bob, _) = await TestUsers.LoggedInAsync(Factory);
        using (alice)
        using (bob)
        {
            await TestClient.EnsureStatusAsync(
                await alice.PutAsync(
                    "/api/users/me/exam-goals/TOEIC",
                    new { targetScore = 900, examDate = FutureDate, dailyStudyMinutes = 90 }),
                HttpStatusCode.OK);

            var bobGoals = await bob.GetJsonAsync<List<ExamGoalDto>>("/api/users/me/exam-goals");

            Assert.Empty(bobGoals);
        }
    }
}
