using System.Data.Common;
using System.Security.Cryptography;
using LanguageLearning.Domain.Attempts;
using LanguageLearning.Domain.Exams;
using LanguageLearning.Domain.QuestionBank;
using LanguageLearning.Infrastructure.Persistence;
using LanguageLearning.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace LanguageLearning.IntegrationTests;

/// <summary>F2-02：題庫與作答資料表的 Migration、索引、刪除規則、不可變防護與並行控制。</summary>
public sealed class QuestionBankPersistenceTests(ApiFixture fixture) : IClassFixture<ApiFixture>
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private ApiFactory Factory => fixture.Factory;

    [Fact]
    public async Task Reference_data_is_seeded()
    {
        await using var scope = Factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var types = await db.QuestionTypes.AsNoTracking().OrderBy(t => t.SortOrder).ToListAsync(Ct);
        Assert.Equal(new[] { 1, 2, 3, 4, 5, 6, 7, 7 }, types.Select(t => t.Part).ToArray());
        Assert.Equal(3, types.Single(t => t.Code == QuestionTypeCodes.QuestionResponse).OptionCount);
        Assert.Equal(QuestionBankReferenceData.Skills.Count, await db.Skills.CountAsync(Ct));
        Assert.Equal(QuestionBankReferenceData.Topics.Count, await db.Topics.CountAsync(Ct));

        var rule = await db.ScoringRules.AsNoTracking().SingleAsync(Ct);
        Assert.True(rule.IsDefault);
        Assert.Equal(QuestionBankReferenceData.ToeicRawScoringRuleId, rule.Id);
        Assert.Empty(await db.Database.GetPendingMigrationsAsync(Ct));
    }

    [Fact]
    public async Task Draft_edits_publish_and_new_version_are_saved()
    {
        var userId = await AdminIdAsync();
        var groupId = await CreateGroupAsync(QuestionBankReferenceData.SinglePassageTypeId, userId, draft =>
        {
            draft.AddPassage("Notice", "The cafeteria will close early on Friday.", Now);
            draft.AddQuestion(Input("Q1?"), Now);
            draft.AddQuestion(Input("Q2?"), Now);
            draft.AddQuestion(Input("Q3?"), Now);
        });

        // 刪除第 1 題（其餘重新編號）並改變正確答案：同一次儲存內交換唯一值。
        await WithGroupAsync(groupId, (group, _) =>
        {
            var draft = group.Draft!;
            draft.RemoveQuestion(draft.Questions.Single(q => q.Number == 1).Id);
            draft.UpdateQuestion(draft.Questions.Single(q => q.Number == 1).Id, Input("Q2?", correctIndex: 2), Now);
        });

        await WithGroupAsync(groupId, (group, type) => group.Publish(type, userId, Now));

        // 建立第 2 版並發布：舊版改為停用、新版改為已發布，在同一次儲存內完成。
        await WithGroupAsync(groupId, (group, type) =>
        {
            var draft = group.CreateDraftFromPublished(userId, Now.AddMinutes(1));
            draft.UpdateContent(null, "修正解析", null);
            group.Publish(type, userId, Now.AddMinutes(1));
        });

        await using var scope = Factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var versions = await db.QuestionGroupVersions.AsNoTracking()
            .Where(v => v.QuestionGroupId == groupId)
            .Include(v => v.Questions).ThenInclude(q => q.Options)
            .OrderBy(v => v.VersionNumber)
            .AsSplitQuery()
            .ToListAsync(Ct);

        Assert.Equal(new[] { ContentStatus.Retired, ContentStatus.Published }, versions.Select(v => v.Status).ToArray());
        Assert.All(versions, v => Assert.Equal(new[] { 1, 2 }, v.Questions.Select(q => q.Number).Order().ToArray()));
        var first = versions[0].Questions.Single(q => q.Number == 1);
        Assert.Equal("Q2?", first.Stem);
        Assert.Equal("C", first.Options.Single(o => o.IsCorrect).Label);
        Assert.Equal("修正解析", versions[1].Explanation);
        Assert.NotEqual(versions[0].ContentHash, versions[1].ContentHash);
    }

    [Fact]
    public async Task Published_content_cannot_be_modified_or_deleted()
    {
        var userId = await AdminIdAsync();
        var groupId = await CreatePublishedPart5Async(userId);

        await AssertImmutableAsync(async db =>
        {
            var question = await db.Questions
                .SingleAsync(q => db.QuestionGroupVersions.Any(v => v.Id == q.GroupVersionId && v.QuestionGroupId == groupId), Ct);
            db.Entry(question).Property(q => q.Stem).CurrentValue = "竄改的題幹";
        });

        await AssertImmutableAsync(async db =>
        {
            var question = await db.Questions.Include(q => q.Options)
                .SingleAsync(q => db.QuestionGroupVersions.Any(v => v.Id == q.GroupVersionId && v.QuestionGroupId == groupId), Ct);
            db.Remove(question.Options.First());
        });

        await AssertImmutableAsync(async db =>
            db.Remove(await db.QuestionGroupVersions.SingleAsync(v => v.QuestionGroupId == groupId, Ct)));

        await AssertImmutableAsync(async db =>
            db.Remove(await db.QuestionGroups.SingleAsync(g => g.Id == groupId, Ct)));

        // 停用是允許的狀態變更。
        await WithGroupAsync(groupId, (group, _) => group.Retire(userId, Now));
        await AssertImmutableAsync(async db =>
        {
            var version = await db.QuestionGroupVersions.SingleAsync(v => v.QuestionGroupId == groupId, Ct);
            db.Entry(version).Property(v => v.Status).CurrentValue = ContentStatus.Published;
        });
    }

    [Fact]
    public async Task Uploaded_media_cannot_be_modified()
    {
        var userId = await AdminIdAsync();
        var asset = NewImage(userId);
        await using (var scope = Factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.MediaAssets.Add(asset);
            await db.SaveChangesAsync(Ct);
        }

        await AssertImmutableAsync(async db =>
        {
            var saved = await db.MediaAssets.SingleAsync(m => m.Id == asset.Id, Ct);
            db.Entry(saved).Property(m => m.License).CurrentValue = "改過的授權";
        });

        // 同一個檔案（相同雜湊）只能有一筆。
        await using (var scope = Factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.MediaAssets.Add(NewImage(userId, asset.Sha256));
            await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync(Ct));
        }
    }

    [Fact]
    public async Task Database_enforces_single_draft_and_unique_option_labels()
    {
        var userId = await AdminIdAsync();
        var groupId = await CreateGroupAsync(QuestionBankReferenceData.IncompleteSentencesTypeId, userId, draft =>
            draft.AddQuestion(Input("The meeting was ------- until Friday."), Now));

        await using var scope = Factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var question = await db.Questions.AsNoTracking()
            .SingleAsync(q => db.QuestionGroupVersions.Any(v => v.Id == q.GroupVersionId && v.QuestionGroupId == groupId), Ct);

        await Assert.ThrowsAnyAsync<DbException>(() => db.Database.ExecuteSqlAsync(
            $"""
            INSERT INTO QuestionGroupVersions (Id, QuestionGroupId, VersionNumber, Status, Source, CreatedAtUtc, CreatedByUserId)
            VALUES ({Guid.NewGuid()}, {groupId}, 2, 'Draft', 'Original', SYSUTCDATETIME(), {userId})
            """,
            Ct));

        await Assert.ThrowsAnyAsync<DbException>(() => db.Database.ExecuteSqlAsync(
            $"UPDATE QuestionOptions SET Label = 'A' WHERE QuestionId = {question.Id}",
            Ct));
    }

    [Fact]
    public async Task Attempt_is_saved_scored_and_then_immutable()
    {
        var userId = await AdminIdAsync();
        var attemptId = await StartPracticeAsync(userId, await CreatePublishedPart5Async(userId));

        await WithAttemptAsync(attemptId, async (db, attempt) =>
        {
            var question = await LoadQuestionAsync(db, attempt.Questions.Single().QuestionId);
            Assert.True(attempt.SaveAnswer(question.Id, question.CorrectOption, 1, false, 8_000, Now.AddSeconds(30)));
        });

        await WithAttemptAsync(attemptId, (_, attempt) =>
        {
            Assert.True(attempt.Submit(Now.AddMinutes(1)));
            return Task.CompletedTask;
        });

        await using (var scope = Factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var saved = await db.ExamAttempts.AsNoTracking()
                .Include(a => a.Answers)
                .SingleAsync(a => a.Id == attemptId, Ct);
            Assert.Equal(AttemptStatus.Completed, saved.Status);
            Assert.Equal(1, saved.CorrectCount);
            Assert.True(Assert.Single(saved.Answers).IsCorrect);
        }

        await AssertImmutableAsync(async db =>
        {
            var attempt = await db.ExamAttempts.SingleAsync(a => a.Id == attemptId, Ct);
            db.Entry(attempt).Property(a => a.CorrectCount).CurrentValue = 99;
        });

        await AssertImmutableAsync(async db =>
        {
            var attempt = await db.ExamAttempts.Include(a => a.Answers).SingleAsync(a => a.Id == attemptId, Ct);
            db.Entry(attempt.Answers.Single()).Property(a => a.SelectedOptionId).CurrentValue = null;
        });
    }

    [Fact]
    public async Task Concurrent_saves_to_same_attempt_conflict()
    {
        var userId = await AdminIdAsync();
        var attemptId = await StartPracticeAsync(
            userId,
            await CreatePublishedPart5Async(userId),
            await CreatePublishedPart5Async(userId));

        await using var first = Factory.Services.CreateAsyncScope();
        await using var second = Factory.Services.CreateAsyncScope();
        var db1 = first.ServiceProvider.GetRequiredService<AppDbContext>();
        var db2 = second.ServiceProvider.GetRequiredService<AppDbContext>();
        var attempt1 = await LoadAttemptAsync(db1, attemptId);
        var attempt2 = await LoadAttemptAsync(db2, attemptId);
        var snapshots = attempt1.Questions.OrderBy(q => q.Sequence).ToList();

        var question1 = await LoadQuestionAsync(db1, snapshots[0].QuestionId);
        var question2 = await LoadQuestionAsync(db2, snapshots[1].QuestionId);
        attempt1.SaveAnswer(question1.Id, question1.CorrectOption, 1, false, null, Now.AddSeconds(1));
        attempt2.SaveAnswer(question2.Id, question2.CorrectOption, 1, false, null, Now.AddSeconds(2));

        await db1.SaveChangesAsync(Ct);
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => db2.SaveChangesAsync(Ct));
    }

    private static readonly DateTime Now = new(2026, 11, 16, 3, 0, 0, DateTimeKind.Utc);

    private static QuestionInput Input(string stem, int correctIndex = 0) =>
        new(
            stem,
            QuestionBankReferenceData.Skills[0].Id,
            Difficulty.Medium,
            "解析",
            ["option a", "option b", "option c", "option d"],
            correctIndex,
            [QuestionBankReferenceData.Topics[0].Id]);

    private static string NewCode() => "IT-" + Guid.NewGuid().ToString("N")[..12].ToUpperInvariant();

    private static MediaAsset NewImage(Guid userId, string? sha256 = null) =>
        MediaAsset.Create(
            MediaKind.Image,
            "image/png",
            1_000,
            sha256 ?? Convert.ToHexStringLower(RandomNumberGenerator.GetBytes(32)),
            $"images/{Guid.NewGuid():N}",
            "chart.png",
            100,
            100,
            null,
            MediaSourceType.Original,
            "原創圖表",
            "專案自有",
            userId,
            Now);

    private async Task<Guid> AdminIdAsync()
    {
        await using var scope = Factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return await db.Users.Where(u => u.Email == ApiFactory.AdminEmail).Select(u => u.Id).SingleAsync(Ct);
    }

    private async Task<Guid> CreateGroupAsync(Guid typeId, Guid userId, Action<QuestionGroupVersion> fill)
    {
        await using var scope = Factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var type = await db.QuestionTypes.SingleAsync(t => t.Id == typeId, Ct);
        var group = QuestionGroup.Create(NewCode(), type, userId, Now);
        fill(group.Draft!);
        db.QuestionGroups.Add(group);
        await db.SaveChangesAsync(Ct);
        return group.Id;
    }

    private async Task<Guid> CreatePublishedPart5Async(Guid userId)
    {
        var groupId = await CreateGroupAsync(QuestionBankReferenceData.IncompleteSentencesTypeId, userId, draft =>
            draft.AddQuestion(Input("The report was ------- on time."), Now));
        await WithGroupAsync(groupId, (group, type) => group.Publish(type, userId, Now));
        return groupId;
    }

    private async Task WithGroupAsync(Guid groupId, Action<QuestionGroup, QuestionType> change)
    {
        await using var scope = Factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var group = await db.QuestionGroups
            .Include(g => g.Versions).ThenInclude(v => v.Questions).ThenInclude(q => q.Options)
            .Include(g => g.Versions).ThenInclude(v => v.Questions).ThenInclude(q => q.Topics)
            .Include(g => g.Versions).ThenInclude(v => v.Passages)
            .Include(g => g.Versions).ThenInclude(v => v.Media)
            .AsSplitQuery()
            .SingleAsync(g => g.Id == groupId, Ct);
        var type = await db.QuestionTypes.SingleAsync(t => t.Id == group.QuestionTypeId, Ct);
        change(group, type);
        await db.SaveChangesAsync(Ct);
    }

    private async Task<Guid> StartPracticeAsync(Guid userId, params Guid[] groupIds)
    {
        await using var scope = Factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var versions = await db.QuestionGroupVersions.AsNoTracking()
            .Where(v => groupIds.Contains(v.QuestionGroupId) && v.Status == ContentStatus.Published)
            .Include(v => v.Questions).ThenInclude(q => q.Options)
            .AsSplitQuery()
            .ToListAsync(Ct);

        var questions = versions
            .OrderBy(v => Array.IndexOf(groupIds, v.QuestionGroupId))
            .SelectMany(v => v.Questions.OrderBy(q => q.Number).Select(q => new AttemptQuestionInfo(
                v.QuestionGroupId,
                v.Id,
                q.Id,
                5,
                ExamSection.Reading,
                q.CorrectOption!.Id)))
            .ToList();

        var attempt = ExamAttempt.StartPractice(userId, QuestionBankReferenceData.ToeicRawScoringRuleId, null, questions, Now);
        db.ExamAttempts.Add(attempt);
        await db.SaveChangesAsync(Ct);
        return attempt.Id;
    }

    private async Task WithAttemptAsync(Guid attemptId, Func<AppDbContext, ExamAttempt, Task> change)
    {
        await using var scope = Factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var attempt = await LoadAttemptAsync(db, attemptId);
        await change(db, attempt);
        await db.SaveChangesAsync(Ct);
    }

    private static Task<ExamAttempt> LoadAttemptAsync(AppDbContext db, Guid attemptId) =>
        db.ExamAttempts
            .Include(a => a.Questions)
            .Include(a => a.Answers)
            .AsSplitQuery()
            .SingleAsync(a => a.Id == attemptId, Ct);

    private static Task<Question> LoadQuestionAsync(AppDbContext db, Guid questionId) =>
        db.Questions.Include(q => q.Options).SingleAsync(q => q.Id == questionId, Ct);

    /// <summary>在新的 scope 中做出違規修改，確認儲存時被不可變防護拒絕。</summary>
    private async Task AssertImmutableAsync(Func<AppDbContext, Task> change)
    {
        await using var scope = Factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await change(db);
        await Assert.ThrowsAsync<ImmutableContentException>(() => db.SaveChangesAsync(Ct));
    }
}
