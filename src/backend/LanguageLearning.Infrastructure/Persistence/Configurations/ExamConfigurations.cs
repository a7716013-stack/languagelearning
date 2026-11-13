using LanguageLearning.Domain.Exams;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LanguageLearning.Infrastructure.Persistence.Configurations;

internal sealed class ExamTypeConfiguration : IEntityTypeConfiguration<ExamType>
{
    public void Configure(EntityTypeBuilder<ExamType> builder)
    {
        builder.ToTable("ExamTypes");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.Code).HasMaxLength(20).IsRequired();
        builder.HasIndex(x => x.Code).IsUnique();
        builder.Property(x => x.Name).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(500).IsRequired();
        builder.Property(x => x.MinScore).HasPrecision(6, 1);
        builder.Property(x => x.MaxScore).HasPrecision(6, 1);
        builder.Property(x => x.ScoreStep).HasPrecision(6, 1);

        builder.HasData(
            new
            {
                Id = SeedData.ToeicExamTypeId,
                Code = ExamTypeCodes.Toeic,
                Name = "TOEIC Listening & Reading",
                Description = "多益聽力與閱讀測驗。總分 10–990 分，以 5 分為單位。",
                IsAvailable = true,
                MinScore = 10m,
                MaxScore = 990m,
                ScoreStep = 5m,
                SortOrder = 1,
            },
            new
            {
                Id = SeedData.IeltsExamTypeId,
                Code = ExamTypeCodes.Ielts,
                Name = "IELTS",
                Description = "雅思。尚未開放，分數制度將於開放前依官方最新公告確認。",
                IsAvailable = false,
                MinScore = 0m,
                MaxScore = 9m,
                ScoreStep = 0.5m,
                SortOrder = 2,
            },
            new
            {
                Id = SeedData.ToeflExamTypeId,
                Code = ExamTypeCodes.Toefl,
                Name = "TOEFL iBT",
                Description = "托福網路測驗。尚未開放，分數制度將於開放前依官方最新公告確認。",
                IsAvailable = false,
                MinScore = 0m,
                MaxScore = 120m,
                ScoreStep = 1m,
                SortOrder = 3,
            });
    }
}

internal sealed class ExamVersionConfiguration : IEntityTypeConfiguration<ExamVersion>
{
    public void Configure(EntityTypeBuilder<ExamVersion> builder)
    {
        builder.ToTable("ExamVersions");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.Code).HasMaxLength(50).IsRequired();
        builder.HasIndex(x => x.Code).IsUnique();
        builder.Property(x => x.Name).HasMaxLength(150).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(1000).IsRequired();
        builder.HasIndex(x => new { x.ExamTypeId, x.IsCurrent });

        builder.HasOne(x => x.ExamType)
            .WithMany(x => x.Versions)
            .HasForeignKey(x => x.ExamTypeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasData(
            new
            {
                Id = SeedData.ToeicLrVersionId,
                ExamTypeId = SeedData.ToeicExamTypeId,
                Code = "TOEIC-LR",
                Name = "TOEIC Listening & Reading（現行題型）",
                Description = "聽力 Part 1–4 共 100 題（約 45 分鐘）；閱讀 Part 5–7 共 100 題（75 分鐘）。完整試卷與計分規則將於 Phase 2 建立。",
                IsCurrent = true,
            });
    }
}
