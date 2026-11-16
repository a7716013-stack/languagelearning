using LanguageLearning.Domain.Exams;
using LanguageLearning.Domain.QuestionBank;
using LanguageLearning.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LanguageLearning.Infrastructure.Persistence.Configurations;

internal sealed class ScoringRuleConfiguration : IEntityTypeConfiguration<ScoringRule>
{
    public void Configure(EntityTypeBuilder<ScoringRule> builder)
    {
        builder.ToTable("ScoringRules");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.Code).HasMaxLength(50).IsRequired();
        builder.Property(x => x.Kind).HasConversion<string>().HasMaxLength(20);
        builder.Property(x => x.Description).HasMaxLength(500).IsRequired();
        builder.HasIndex(x => new { x.Code, x.Version }).IsUnique();

        builder.HasOne<ExamVersion>()
            .WithMany()
            .HasForeignKey(x => x.ExamVersionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasData(QuestionBankReferenceData.ScoringRules);
    }
}

internal sealed class ExamPaperConfiguration : IEntityTypeConfiguration<ExamPaper>
{
    public void Configure(EntityTypeBuilder<ExamPaper> builder)
    {
        builder.ToTable("ExamPapers");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.Code).HasMaxLength(ExamPaper.CodeMaxLength).IsRequired();
        builder.HasIndex(x => x.Code).IsUnique();
        builder.Property(x => x.Title).HasMaxLength(ExamPaper.TitleMaxLength).IsRequired();
        builder.Property(x => x.Kind).HasConversion<string>().HasMaxLength(20);
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(x => x.RowVersion).IsRowVersion();
        builder.Ignore(x => x.TotalQuestions);
        builder.HasIndex(x => new { x.Kind, x.Status });

        builder.HasOne<ExamVersion>()
            .WithMany()
            .HasForeignKey(x => x.ExamVersionId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ScoringRule>()
            .WithMany()
            .HasForeignKey(x => x.DefaultScoringRuleId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(x => x.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(x => x.PublishedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(x => x.RetiredByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        // 試卷項目只會隨草稿試卷一起刪除；已發布試卷由 ImmutableContentInterceptor 禁止刪除。
        builder.HasMany(x => x.Items)
            .WithOne()
            .HasForeignKey(x => x.ExamPaperId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(x => x.Items).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

internal sealed class ExamPaperItemConfiguration : IEntityTypeConfiguration<ExamPaperItem>
{
    public void Configure(EntityTypeBuilder<ExamPaperItem> builder)
    {
        builder.ToTable("ExamPaperItems");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.HasIndex(x => new { x.ExamPaperId, x.SortOrder }).IsUnique();
        builder.HasIndex(x => new { x.ExamPaperId, x.GroupVersionId }).IsUnique();
        builder.HasIndex(x => new { x.ExamPaperId, x.QuestionGroupId }).IsUnique();

        builder.HasOne<QuestionGroup>()
            .WithMany()
            .HasForeignKey(x => x.QuestionGroupId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<QuestionGroupVersion>()
            .WithMany()
            .HasForeignKey(x => x.GroupVersionId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<QuestionType>()
            .WithMany()
            .HasForeignKey(x => x.QuestionTypeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
