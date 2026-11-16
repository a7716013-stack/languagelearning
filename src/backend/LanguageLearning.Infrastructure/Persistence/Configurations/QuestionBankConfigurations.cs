using LanguageLearning.Domain.Exams;
using LanguageLearning.Domain.QuestionBank;
using LanguageLearning.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LanguageLearning.Infrastructure.Persistence.Configurations;

internal sealed class QuestionTypeConfiguration : IEntityTypeConfiguration<QuestionType>
{
    public void Configure(EntityTypeBuilder<QuestionType> builder)
    {
        builder.ToTable("QuestionTypes");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.Code).HasMaxLength(40).IsRequired();
        builder.HasIndex(x => x.Code).IsUnique();
        builder.Property(x => x.Name).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Section).HasConversion<string>().HasMaxLength(20);

        builder.HasOne<ExamVersion>()
            .WithMany()
            .HasForeignKey(x => x.ExamVersionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasData(QuestionBankReferenceData.QuestionTypes);
    }
}

internal sealed class SkillConfiguration : IEntityTypeConfiguration<Skill>
{
    public void Configure(EntityTypeBuilder<Skill> builder)
    {
        builder.ToTable("Skills");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.Code).HasMaxLength(40).IsRequired();
        builder.HasIndex(x => x.Code).IsUnique();
        builder.Property(x => x.Name).HasMaxLength(50).IsRequired();
        builder.Property(x => x.Section).HasConversion<string>().HasMaxLength(20);

        builder.HasData(QuestionBankReferenceData.Skills);
    }
}

internal sealed class TopicConfiguration : IEntityTypeConfiguration<Topic>
{
    public void Configure(EntityTypeBuilder<Topic> builder)
    {
        builder.ToTable("Topics");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.Code).HasMaxLength(40).IsRequired();
        builder.HasIndex(x => x.Code).IsUnique();
        builder.Property(x => x.Name).HasMaxLength(50).IsRequired();

        builder.HasData(QuestionBankReferenceData.Topics);
    }
}

internal sealed class QuestionGroupConfiguration : IEntityTypeConfiguration<QuestionGroup>
{
    public void Configure(EntityTypeBuilder<QuestionGroup> builder)
    {
        builder.ToTable("QuestionGroups");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.Code).HasMaxLength(QuestionBankRules.GroupCodeMaxLength).IsRequired();
        builder.HasIndex(x => x.Code).IsUnique();
        builder.Property(x => x.RowVersion).IsRowVersion();
        builder.Ignore(x => x.Draft);
        builder.Ignore(x => x.PublishedVersion);

        builder.HasOne(x => x.QuestionType)
            .WithMany()
            .HasForeignKey(x => x.QuestionTypeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(x => x.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        // ClientCascade：捨棄草稿時由 EF 刪除該版本；資料庫端為 NoAction，避免刪除題組時連帶刪掉已發布版本。
        builder.HasMany(x => x.Versions)
            .WithOne()
            .HasForeignKey(x => x.QuestionGroupId)
            .OnDelete(DeleteBehavior.ClientCascade);
        builder.Navigation(x => x.Versions).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

internal sealed class QuestionGroupVersionConfiguration : IEntityTypeConfiguration<QuestionGroupVersion>
{
    public void Configure(EntityTypeBuilder<QuestionGroupVersion> builder)
    {
        builder.ToTable("QuestionGroupVersions");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(x => x.Source).HasConversion<string>().HasMaxLength(20);
        builder.Property(x => x.Transcript).HasMaxLength(QuestionBankRules.LongTextMaxLength);
        builder.Property(x => x.Explanation).HasMaxLength(QuestionBankRules.LongTextMaxLength);
        builder.Property(x => x.AuthorNote).HasMaxLength(QuestionBankRules.AuthorNoteMaxLength);
        builder.Property(x => x.ContentHash).HasMaxLength(64).IsFixedLength().IsUnicode(false);
        builder.Property(x => x.RowVersion).IsRowVersion();
        builder.Ignore(x => x.IsDraft);

        builder.HasIndex(x => new { x.QuestionGroupId, x.VersionNumber }).IsUnique();

        // 每個題組最多一個草稿（ADR 0008）。「最多一個已發布版本」由 Domain 保證，
        // 並以 QuestionGroups.RowVersion 讓並行發布只有一個成功：發布會同時把舊版改為停用，
        // 若用篩選唯一索引，同一次儲存內兩筆 UPDATE 的執行順序會決定成敗。
        builder.HasIndex(x => x.QuestionGroupId, "IX_QuestionGroupVersions_QuestionGroupId_Draft")
            .IsUnique()
            .HasFilter("[Status] = 'Draft'");
        builder.HasIndex(x => new { x.QuestionGroupId, x.Status });
        builder.HasIndex(x => x.Status);

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

        // 子資料只會隨草稿一起刪除；已發布版本由 ImmutableContentInterceptor 禁止刪除。
        builder.HasMany(x => x.Passages)
            .WithOne()
            .HasForeignKey(x => x.GroupVersionId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(x => x.Questions)
            .WithOne()
            .HasForeignKey(x => x.GroupVersionId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(x => x.Media)
            .WithOne()
            .HasForeignKey(x => x.GroupVersionId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(x => x.Passages).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Navigation(x => x.Questions).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Navigation(x => x.Media).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

internal sealed class QuestionGroupPassageConfiguration : IEntityTypeConfiguration<QuestionGroupPassage>
{
    public void Configure(EntityTypeBuilder<QuestionGroupPassage> builder)
    {
        builder.ToTable("QuestionGroupPassages");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.Title).HasMaxLength(QuestionBankRules.PassageTitleMaxLength);
        builder.Property(x => x.Body).HasMaxLength(QuestionBankRules.LongTextMaxLength).IsRequired();
        builder.HasIndex(x => new { x.GroupVersionId, x.SortOrder }).IsUnique();
    }
}

internal sealed class QuestionConfiguration : IEntityTypeConfiguration<Question>
{
    public void Configure(EntityTypeBuilder<Question> builder)
    {
        builder.ToTable("Questions");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.Stem).HasMaxLength(QuestionBankRules.StemMaxLength);
        builder.Property(x => x.Explanation).HasMaxLength(QuestionBankRules.LongTextMaxLength).IsRequired();
        builder.Property(x => x.IntendedDifficulty).HasConversion<string>().HasMaxLength(10);
        builder.Ignore(x => x.CorrectOption);
        builder.HasIndex(x => new { x.GroupVersionId, x.Number }).IsUnique();

        builder.HasOne<Skill>()
            .WithMany()
            .HasForeignKey(x => x.SkillId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(x => x.Options)
            .WithOne()
            .HasForeignKey(x => x.QuestionId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(x => x.Topics)
            .WithOne()
            .HasForeignKey(x => x.QuestionId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(x => x.Options).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Navigation(x => x.Topics).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

internal sealed class QuestionOptionConfiguration : IEntityTypeConfiguration<QuestionOption>
{
    public void Configure(EntityTypeBuilder<QuestionOption> builder)
    {
        builder.ToTable("QuestionOptions");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.Label).HasMaxLength(1).IsFixedLength().IsUnicode(false).IsRequired();
        builder.Property(x => x.Text).HasMaxLength(QuestionBankRules.OptionTextMaxLength).IsRequired();
        // 「恰好一個正確答案」由 Domain 在發布時檢查；草稿修改正確答案會在同一次儲存內更新兩個選項，
        // 不使用篩選唯一索引以免受 UPDATE 執行順序影響。發布後內容不可變。
        builder.HasIndex(x => new { x.QuestionId, x.Label }).IsUnique();
    }
}

internal sealed class QuestionTopicConfiguration : IEntityTypeConfiguration<QuestionTopic>
{
    public void Configure(EntityTypeBuilder<QuestionTopic> builder)
    {
        builder.ToTable("QuestionTopics");
        builder.HasKey(x => new { x.QuestionId, x.TopicId });

        builder.HasOne<Topic>()
            .WithMany()
            .HasForeignKey(x => x.TopicId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class QuestionGroupMediaConfiguration : IEntityTypeConfiguration<QuestionGroupMedia>
{
    public void Configure(EntityTypeBuilder<QuestionGroupMedia> builder)
    {
        builder.ToTable("QuestionGroupMedia");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.Purpose).HasConversion<string>().HasMaxLength(20);
        builder.Property(x => x.AltText).HasMaxLength(QuestionBankRules.AltTextMaxLength);
        // 「每個版本最多一個聽力音檔」由 Domain 保證（更換音檔是同一次儲存內的刪除加新增）。
        builder.HasIndex(x => new { x.GroupVersionId, x.MediaAssetId }).IsUnique();

        builder.HasOne(x => x.MediaAsset)
            .WithMany()
            .HasForeignKey(x => x.MediaAssetId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class MediaAssetConfiguration : IEntityTypeConfiguration<MediaAsset>
{
    public void Configure(EntityTypeBuilder<MediaAsset> builder)
    {
        builder.ToTable("MediaAssets");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.Kind).HasConversion<string>().HasMaxLength(10);
        builder.Property(x => x.ContentType).HasMaxLength(MediaRules.ContentTypeMaxLength).IsUnicode(false).IsRequired();
        builder.Property(x => x.Sha256).HasMaxLength(64).IsFixedLength().IsUnicode(false).IsRequired();
        builder.Property(x => x.StorageKey).HasMaxLength(MediaRules.StorageKeyMaxLength).IsUnicode(false).IsRequired();
        builder.Property(x => x.OriginalFileName).HasMaxLength(MediaRules.FileNameMaxLength).IsRequired();
        builder.Property(x => x.SourceType).HasConversion<string>().HasMaxLength(20);
        builder.Property(x => x.SourceDescription).HasMaxLength(MediaRules.SourceDescriptionMaxLength).IsRequired();
        builder.Property(x => x.License).HasMaxLength(MediaRules.LicenseMaxLength).IsRequired();

        // 同一個檔案重複上傳時重用既有紀錄。
        builder.HasIndex(x => new { x.Kind, x.Sha256 }).IsUnique();
        builder.HasIndex(x => x.StorageKey).IsUnique();

        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(x => x.UploadedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
