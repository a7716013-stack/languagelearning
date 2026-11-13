using LanguageLearning.Domain.Auditing;
using LanguageLearning.Domain.Exams;
using LanguageLearning.Domain.Goals;
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

    EntityEntry<TEntity> Entry<TEntity>(TEntity entity)
        where TEntity : class;

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
