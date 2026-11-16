using LanguageLearning.Domain.Attempts;
using LanguageLearning.Domain.Exams;
using LanguageLearning.Domain.QuestionBank;
using LanguageLearning.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LanguageLearning.Infrastructure.Persistence.Configurations;

internal sealed class ExamAttemptConfiguration : IEntityTypeConfiguration<ExamAttempt>
{
    public void Configure(EntityTypeBuilder<ExamAttempt> builder)
    {
        builder.ToTable("ExamAttempts");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.Mode).HasConversion<string>().HasMaxLength(20);
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(x => x.CompletionReason).HasConversion<string>().HasMaxLength(20);
        builder.Property(x => x.CurrentSection).HasConversion<string>().HasMaxLength(20);
        builder.Property(x => x.PracticeCriteria).HasMaxLength(AttemptRules.PracticeCriteriaMaxLength);
        builder.Property(x => x.RowVersion).IsRowVersion();
        builder.Ignore(x => x.FinalDeadlineUtc);

        builder.HasIndex(x => new { x.UserId, x.StartedAtUtc });
        builder.HasIndex(x => new { x.UserId, x.Status });

        // 背景服務每分鐘掃描逾時的作答。
        builder.HasIndex(x => new { x.Status, x.ListeningDeadlineUtc, x.ReadingDeadlineUtc })
            .HasFilter("[Status] = 'InProgress'");

        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ExamPaper>()
            .WithMany()
            .HasForeignKey(x => x.ExamPaperId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ScoringRule>()
            .WithMany()
            .HasForeignKey(x => x.ScoringRuleId)
            .OnDelete(DeleteBehavior.Restrict);

        // 作答紀錄不刪除（ImmutableContentInterceptor 禁止）；Cascade 只是讓關聯明確。
        builder.HasMany(x => x.Questions)
            .WithOne()
            .HasForeignKey(x => x.AttemptId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(x => x.Answers)
            .WithOne()
            .HasForeignKey(x => x.AttemptId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(x => x.Questions).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Navigation(x => x.Answers).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

internal sealed class AttemptQuestionSnapshotConfiguration : IEntityTypeConfiguration<AttemptQuestionSnapshot>
{
    public void Configure(EntityTypeBuilder<AttemptQuestionSnapshot> builder)
    {
        builder.ToTable("AttemptQuestionSnapshots");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.Section).HasConversion<string>().HasMaxLength(20);
        builder.HasIndex(x => new { x.AttemptId, x.Sequence }).IsUnique();
        builder.HasIndex(x => new { x.AttemptId, x.QuestionId }).IsUnique();

        // 查詢使用者做過的題目（重複題提示）。
        builder.HasIndex(x => x.QuestionGroupId);

        builder.HasOne<QuestionGroup>()
            .WithMany()
            .HasForeignKey(x => x.QuestionGroupId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<QuestionGroupVersion>()
            .WithMany()
            .HasForeignKey(x => x.GroupVersionId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Question>()
            .WithMany()
            .HasForeignKey(x => x.QuestionId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<QuestionOption>()
            .WithMany()
            .HasForeignKey(x => x.CorrectOptionId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class AttemptAnswerConfiguration : IEntityTypeConfiguration<AttemptAnswer>
{
    public void Configure(EntityTypeBuilder<AttemptAnswer> builder)
    {
        builder.ToTable("AttemptAnswers");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.HasIndex(x => x.SnapshotId).IsUnique();

        // 與 ExamAttempt → AttemptAnswer 的 Cascade 並存時，SQL Server 不允許第二條串聯路徑。
        builder.HasOne<AttemptQuestionSnapshot>()
            .WithMany()
            .HasForeignKey(x => x.SnapshotId)
            .OnDelete(DeleteBehavior.NoAction);
        builder.HasOne<QuestionOption>()
            .WithMany()
            .HasForeignKey(x => x.SelectedOptionId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
