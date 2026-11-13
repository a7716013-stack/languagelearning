using LanguageLearning.Application.Abstractions;
using LanguageLearning.Application.Auth;
using LanguageLearning.Application.Common.Auditing;
using LanguageLearning.Application.Common.Results;
using LanguageLearning.Application.Common.Time;
using LanguageLearning.Domain.Auditing;
using LanguageLearning.Domain.Common;
using LanguageLearning.Domain.Goals;
using Microsoft.EntityFrameworkCore;

namespace LanguageLearning.Application.Goals;

/// <summary>使用者學習目標（Onboarding 與設定頁使用）。只能存取目前登入者自己的資料。</summary>
public sealed class ExamGoalService(
    IAppDbContext db,
    ICurrentUser currentUser,
    IUserTimeZoneResolver timeZoneResolver,
    TimeProvider timeProvider,
    IAuditLogger auditLogger)
{
    public static readonly AppError GoalConflict = AppError.Conflict(
        "goal_conflict",
        "學習目標已在其他地方更新，請重新整理後再試。");

    public static readonly AppError ExamTypeNotFound = AppError.NotFound(
        "exam_type_not_found",
        "找不到指定的考試類型。");

    public async Task<Result<IReadOnlyList<ExamGoalDto>>> GetMyGoalsAsync(CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } userId)
        {
            return AuthErrors.NotAuthenticated;
        }

        var today = await GetTodayAsync(userId, cancellationToken);
        var goals = await db.UserExamGoals
            .AsNoTracking()
            .Include(x => x.ExamType)
            .Where(x => x.UserId == userId)
            .OrderBy(x => x.ExamType!.SortOrder)
            .ToListAsync(cancellationToken);

        IReadOnlyList<ExamGoalDto> result = goals.Select(goal => ToDto(goal, today)).ToList();
        return Result<IReadOnlyList<ExamGoalDto>>.Success(result);
    }

    public async Task<Result<ExamGoalDto>> UpsertAsync(
        string examTypeCode,
        UpsertExamGoalRequest request,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } userId)
        {
            return AuthErrors.NotAuthenticated;
        }

        var code = examTypeCode.Trim().ToUpperInvariant();
        var examType = await db.ExamTypes.SingleOrDefaultAsync(x => x.Code == code, cancellationToken);
        if (examType is null)
        {
            return ExamTypeNotFound;
        }

        var now = timeProvider.GetUtcNow();
        var today = await GetTodayAsync(userId, cancellationToken);
        var goal = await db.UserExamGoals
            .SingleOrDefaultAsync(x => x.UserId == userId && x.ExamTypeId == examType.Id, cancellationToken);

        try
        {
            if (goal is null)
            {
                if (!string.IsNullOrEmpty(request.Version))
                {
                    return GoalConflict;
                }

                goal = UserExamGoal.Create(
                    userId,
                    examType,
                    request.TargetScore,
                    request.ExamDate,
                    request.DailyStudyMinutes,
                    today,
                    now.UtcDateTime);
                db.UserExamGoals.Add(goal);
            }
            else
            {
                if (!TryDecodeVersion(request.Version, out var expectedVersion)
                    || !expectedVersion.AsSpan().SequenceEqual(goal.RowVersion))
                {
                    return GoalConflict;
                }

                db.Entry(goal).Property(x => x.RowVersion).OriginalValue = expectedVersion;
                goal.Update(
                    examType,
                    request.TargetScore,
                    request.ExamDate,
                    request.DailyStudyMinutes,
                    today,
                    now.UtcDateTime);
            }
        }
        catch (DomainException ex)
        {
            return AppError.Validation(ex.Code, ex.Message, ex.Field);
        }

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return GoalConflict;
        }
        catch (DbUpdateException)
        {
            // 同一使用者同時建立同一考試目標時，唯一索引會擋下第二筆。
            return GoalConflict;
        }

        await auditLogger.WriteAsync(
            new AuditEntry(
                AuditActions.ExamGoalSaved,
                Succeeded: true,
                ActorUserId: userId,
                EntityType: nameof(UserExamGoal),
                EntityId: goal.Id.ToString(),
                Details: new Dictionary<string, object?>
                {
                    ["examType"] = examType.Code,
                    ["targetScore"] = goal.TargetScore,
                    ["examDate"] = goal.ExamDate,
                    ["dailyStudyMinutes"] = goal.DailyStudyMinutes,
                }),
            cancellationToken);

        return ToDto(goal, today);
    }

    private async Task<DateOnly> GetTodayAsync(Guid userId, CancellationToken cancellationToken)
    {
        var timeZoneId = await timeZoneResolver.GetTimeZoneIdAsync(userId, cancellationToken);
        return TimeZoneHelper.TodayIn(timeZoneId, timeProvider.GetUtcNow());
    }

    private static bool TryDecodeVersion(string? version, out byte[] bytes)
    {
        bytes = [];
        if (string.IsNullOrWhiteSpace(version))
        {
            return false;
        }

        try
        {
            bytes = Convert.FromBase64String(version);
            return bytes.Length > 0;
        }
        catch (FormatException)
        {
            return false;
        }
    }

    private static ExamGoalDto ToDto(UserExamGoal goal, DateOnly today) =>
        new(
            goal.ExamType?.Code ?? string.Empty,
            goal.ExamType?.Name ?? string.Empty,
            goal.TargetScore,
            goal.ExamDate,
            goal.DailyStudyMinutes,
            goal.DaysUntilExam(today),
            goal.UpdatedAtUtc,
            Convert.ToBase64String(goal.RowVersion));
}
