using LanguageLearning.Application.Abstractions;
using LanguageLearning.Domain.Attempts;
using LanguageLearning.Domain.Auditing;
using LanguageLearning.Domain.Exams;
using LanguageLearning.Domain.Goals;
using LanguageLearning.Domain.QuestionBank;
using LanguageLearning.Infrastructure.Email;
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

    public DbSet<EmailOutboxMessage> EmailOutbox => Set<EmailOutboxMessage>();

    public DbSet<QuestionType> QuestionTypes => Set<QuestionType>();

    public DbSet<Skill> Skills => Set<Skill>();

    public DbSet<Topic> Topics => Set<Topic>();

    public DbSet<QuestionGroup> QuestionGroups => Set<QuestionGroup>();

    public DbSet<QuestionGroupVersion> QuestionGroupVersions => Set<QuestionGroupVersion>();

    public DbSet<Question> Questions => Set<Question>();

    public DbSet<MediaAsset> MediaAssets => Set<MediaAsset>();

    public DbSet<ScoringRule> ScoringRules => Set<ScoringRule>();

    public DbSet<ExamPaper> ExamPapers => Set<ExamPaper>();

    public DbSet<ExamAttempt> ExamAttempts => Set<ExamAttempt>();

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
