using LanguageLearning.Application.Abstractions;
using LanguageLearning.Domain.Auditing;
using LanguageLearning.Domain.Exams;
using LanguageLearning.Domain.Goals;
using LanguageLearning.Infrastructure.Identity;
using LanguageLearning.Infrastructure.Persistence.Converters;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace LanguageLearning.Infrastructure.Persistence;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options)
    : IdentityDbContext<ApplicationUser, ApplicationRole, Guid>(options), IAppDbContext
{
    public DbSet<ExamType> ExamTypes => Set<ExamType>();

    public DbSet<ExamVersion> ExamVersions => Set<ExamVersion>();

    public DbSet<UserExamGoal> UserExamGoals => Set<UserExamGoal>();

    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Properties<DateTime>().HaveConversion<UtcDateTimeConverter>();
        configurationBuilder.Properties<DateTime?>().HaveConversion<UtcDateTimeConverter>();
    }
}
