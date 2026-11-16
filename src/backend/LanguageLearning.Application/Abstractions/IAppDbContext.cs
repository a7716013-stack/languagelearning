using LanguageLearning.Domain.Attempts;
using LanguageLearning.Domain.Auditing;
using LanguageLearning.Domain.Exams;
using LanguageLearning.Domain.Goals;
using LanguageLearning.Domain.QuestionBank;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace LanguageLearning.Application.Abstractions;

/// <summary>
/// Application 層使用的資料存取介面，由 Infrastructure 的 EF Core DbContext 實作。
/// </summary>
public interface IAppDbContext
{
    DbSet<ExamType> ExamTypes { get; }

    DbSet<ExamVersion> ExamVersions { get; }

    DbSet<UserExamGoal> UserExamGoals { get; }

    DbSet<AuditLog> AuditLogs { get; }

    DbSet<QuestionType> QuestionTypes { get; }

    DbSet<Skill> Skills { get; }

    DbSet<Topic> Topics { get; }

    DbSet<QuestionGroup> QuestionGroups { get; }

    DbSet<QuestionGroupVersion> QuestionGroupVersions { get; }

    DbSet<Question> Questions { get; }

    DbSet<MediaAsset> MediaAssets { get; }

    DbSet<ScoringRule> ScoringRules { get; }

    DbSet<ExamPaper> ExamPapers { get; }

    DbSet<ExamAttempt> ExamAttempts { get; }

    EntityEntry<TEntity> Entry<TEntity>(TEntity entity)
        where TEntity : class;

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
