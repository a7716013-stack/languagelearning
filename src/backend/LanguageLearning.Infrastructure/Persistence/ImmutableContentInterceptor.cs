using LanguageLearning.Domain.Attempts;
using LanguageLearning.Domain.Exams;
using LanguageLearning.Domain.QuestionBank;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace LanguageLearning.Infrastructure.Persistence;

/// <summary>違反「已發布內容與完成的作答不可變」時拋出；代表程式錯誤，不是使用者可修正的狀況。</summary>
public sealed class ImmutableContentException(string message) : InvalidOperationException(message);

/// <summary>
/// 儲存前再檢查一次不可變規則（ADR 0008），防止程式錯誤繞過 Domain 方法修改歷史資料：
/// 已發布或停用的題組版本與其內容、已上傳的媒體、已發布的試卷、完成的作答與作答快照。
/// </summary>
internal sealed class ImmutableContentInterceptor : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        if (eventData.Context is { } context)
        {
            new Validator(context).ValidateAsync(CancellationToken.None).GetAwaiter().GetResult();
        }

        return result;
    }

    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context is { } context)
        {
            await new Validator(context).ValidateAsync(cancellationToken);
        }

        return result;
    }

    private sealed class Validator(DbContext context)
    {
        /// <summary>停用時允許變更的欄位。</summary>
        private static readonly HashSet<string> RetireProperties = new(StringComparer.Ordinal)
        {
            nameof(QuestionGroupVersion.Status),
            nameof(QuestionGroupVersion.RetiredAtUtc),
            nameof(QuestionGroupVersion.RetiredByUserId),
        };

        private readonly Dictionary<Guid, bool> _versionIsDraft = [];
        private readonly Dictionary<Guid, Guid?> _questionVersion = [];
        private readonly Dictionary<Guid, bool> _paperIsDraft = [];
        private readonly Dictionary<Guid, bool> _attemptInProgress = [];

        public async Task ValidateAsync(CancellationToken cancellationToken)
        {
            context.ChangeTracker.DetectChanges();
            var entries = context.ChangeTracker.Entries()
                .Where(e => e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
                .ToList();

            foreach (var entry in entries)
            {
                switch (entry.Entity)
                {
                    case QuestionGroup group when entry.State == EntityState.Deleted:
                        if (await context.Set<QuestionGroupVersion>().AsNoTracking()
                                .AnyAsync(v => v.QuestionGroupId == group.Id && v.Status != ContentStatus.Draft, cancellationToken))
                        {
                            Fail("已發布或停用過的題組不可刪除。");
                        }

                        break;

                    case QuestionGroupVersion:
                        CheckStatusEntity(entry, "題組版本");
                        break;

                    case QuestionGroupPassage or Question or QuestionGroupMedia:
                        await RequireDraftVersionAsync(GetGuid(entry, nameof(Question.GroupVersionId)), cancellationToken);
                        break;

                    case QuestionOption or QuestionTopic:
                        var versionId = await GetVersionOfQuestionAsync(
                            GetGuid(entry, nameof(QuestionOption.QuestionId)),
                            cancellationToken);
                        await RequireDraftVersionAsync(versionId, cancellationToken);
                        break;

                    case MediaAsset when entry.State != EntityState.Added:
                        Fail("已上傳的媒體不可修改或刪除。");
                        break;

                    case ExamPaper:
                        CheckStatusEntity(entry, "試卷");
                        break;

                    case ExamPaperItem:
                        if (!await IsPaperDraftAsync(GetGuid(entry, nameof(ExamPaperItem.ExamPaperId)), cancellationToken))
                        {
                            Fail("已發布或停用的試卷不可修改。");
                        }

                        break;

                    case ExamAttempt:
                        CheckAttempt(entry);
                        break;

                    case AttemptQuestionSnapshot:
                        if (entry.State != EntityState.Added
                            || !IsAddedAttempt(GetGuid(entry, nameof(AttemptQuestionSnapshot.AttemptId))))
                        {
                            Fail("作答題目只能在開始作答時建立，之後不可修改。");
                        }

                        break;

                    case AttemptAnswer:
                        if (entry.State == EntityState.Deleted
                            || !await IsAttemptInProgressAsync(GetGuid(entry, nameof(AttemptAnswer.AttemptId)), cancellationToken))
                        {
                            Fail("已完成的作答不可修改或刪除。");
                        }

                        break;
                }
            }
        }

        /// <summary>
        /// 題組版本與試卷：新增時只能是草稿或已發布（建立後立即發布）；只有草稿可以修改或刪除；已發布只能改為停用。
        /// </summary>
        private static void CheckStatusEntity(EntityEntry entry, string name)
        {
            var current = (ContentStatus)entry.Property(nameof(QuestionGroupVersion.Status)).CurrentValue!;
            switch (entry.State)
            {
                case EntityState.Added when current == ContentStatus.Retired:
                    Fail($"新的{name}不可直接建立為停用。");
                    break;

                case EntityState.Deleted when OriginalStatus(entry) != ContentStatus.Draft:
                    Fail($"已發布或停用的{name}不可刪除。");
                    break;

                case EntityState.Modified:
                    var original = OriginalStatus(entry);
                    if (original == ContentStatus.Draft)
                    {
                        break;
                    }

                    var modified = entry.Properties.Where(p => p.IsModified).Select(p => p.Metadata.Name);
                    if (original == ContentStatus.Published
                        && current == ContentStatus.Retired
                        && modified.All(RetireProperties.Contains))
                    {
                        break;
                    }

                    Fail($"已發布或停用的{name}不可修改，請建立新版本。");
                    break;
            }
        }

        private static void CheckAttempt(EntityEntry entry)
        {
            switch (entry.State)
            {
                case EntityState.Deleted:
                    Fail("作答紀錄不可刪除。");
                    break;

                case EntityState.Modified
                    when (AttemptStatus)entry.Property(nameof(ExamAttempt.Status)).OriginalValue! != AttemptStatus.InProgress:
                    Fail("已完成的作答不可修改。");
                    break;
            }
        }

        private static ContentStatus OriginalStatus(EntityEntry entry) =>
            (ContentStatus)entry.Property(nameof(QuestionGroupVersion.Status)).OriginalValue!;

        private static Guid GetGuid(EntityEntry entry, string propertyName)
        {
            var property = entry.Property(propertyName);
            return (Guid)(entry.State == EntityState.Deleted ? property.OriginalValue : property.CurrentValue)!;
        }

        private static void Fail(string message) => throw new ImmutableContentException(message);

        private async Task RequireDraftVersionAsync(Guid? versionId, CancellationToken cancellationToken)
        {
            if (versionId is not { } id || !await IsVersionDraftAsync(id, cancellationToken))
            {
                Fail("已發布或停用的題組版本內容不可修改，請建立新版本。");
            }
        }

        private async Task<bool> IsVersionDraftAsync(Guid versionId, CancellationToken cancellationToken)
        {
            if (_versionIsDraft.TryGetValue(versionId, out var cached))
            {
                return cached;
            }

            var tracked = context.ChangeTracker.Entries<QuestionGroupVersion>().FirstOrDefault(e => e.Entity.Id == versionId);
            bool isDraft;
            if (tracked is not null)
            {
                isDraft = tracked.State == EntityState.Added
                    || tracked.Property(v => v.Status).OriginalValue == ContentStatus.Draft;
            }
            else
            {
                var status = await context.Set<QuestionGroupVersion>().AsNoTracking()
                    .Where(v => v.Id == versionId)
                    .Select(v => (ContentStatus?)v.Status)
                    .FirstOrDefaultAsync(cancellationToken);
                isDraft = status == ContentStatus.Draft;
            }

            _versionIsDraft[versionId] = isDraft;
            return isDraft;
        }

        private async Task<Guid?> GetVersionOfQuestionAsync(Guid questionId, CancellationToken cancellationToken)
        {
            if (_questionVersion.TryGetValue(questionId, out var cached))
            {
                return cached;
            }

            var tracked = context.ChangeTracker.Entries<Question>().FirstOrDefault(e => e.Entity.Id == questionId);
            Guid? versionId = tracked is not null
                ? tracked.Property(q => q.GroupVersionId).OriginalValue
                : await context.Set<Question>().AsNoTracking()
                    .Where(q => q.Id == questionId)
                    .Select(q => (Guid?)q.GroupVersionId)
                    .FirstOrDefaultAsync(cancellationToken);

            _questionVersion[questionId] = versionId;
            return versionId;
        }

        private async Task<bool> IsPaperDraftAsync(Guid paperId, CancellationToken cancellationToken)
        {
            if (_paperIsDraft.TryGetValue(paperId, out var cached))
            {
                return cached;
            }

            var tracked = context.ChangeTracker.Entries<ExamPaper>().FirstOrDefault(e => e.Entity.Id == paperId);
            bool isDraft;
            if (tracked is not null)
            {
                isDraft = tracked.State == EntityState.Added
                    || tracked.Property(p => p.Status).OriginalValue == ContentStatus.Draft;
            }
            else
            {
                var status = await context.Set<ExamPaper>().AsNoTracking()
                    .Where(p => p.Id == paperId)
                    .Select(p => (ContentStatus?)p.Status)
                    .FirstOrDefaultAsync(cancellationToken);
                isDraft = status == ContentStatus.Draft;
            }

            _paperIsDraft[paperId] = isDraft;
            return isDraft;
        }

        private bool IsAddedAttempt(Guid attemptId) =>
            context.ChangeTracker.Entries<ExamAttempt>()
                .Any(e => e.Entity.Id == attemptId && e.State == EntityState.Added);

        private async Task<bool> IsAttemptInProgressAsync(Guid attemptId, CancellationToken cancellationToken)
        {
            if (_attemptInProgress.TryGetValue(attemptId, out var cached))
            {
                return cached;
            }

            var tracked = context.ChangeTracker.Entries<ExamAttempt>().FirstOrDefault(e => e.Entity.Id == attemptId);
            bool inProgress;
            if (tracked is not null)
            {
                inProgress = tracked.State == EntityState.Added
                    || tracked.Property(a => a.Status).OriginalValue == AttemptStatus.InProgress;
            }
            else
            {
                var status = await context.Set<ExamAttempt>().AsNoTracking()
                    .Where(a => a.Id == attemptId)
                    .Select(a => (AttemptStatus?)a.Status)
                    .FirstOrDefaultAsync(cancellationToken);
                inProgress = status == AttemptStatus.InProgress;
            }

            _attemptInProgress[attemptId] = inProgress;
            return inProgress;
        }
    }
}
