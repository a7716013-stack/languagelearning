using LanguageLearning.Domain.Auditing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LanguageLearning.Infrastructure.Persistence.Configurations;

internal sealed class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("AuditLogs");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Action).HasMaxLength(100).IsRequired();
        builder.Property(x => x.EntityType).HasMaxLength(100);
        builder.Property(x => x.EntityId).HasMaxLength(100);
        builder.Property(x => x.IpAddress).HasMaxLength(45);
        builder.Property(x => x.Details).HasMaxLength(2000);
        builder.HasIndex(x => x.OccurredAtUtc);
        builder.HasIndex(x => x.ActorUserId);
        builder.HasIndex(x => new { x.Action, x.OccurredAtUtc });

        // 稽核紀錄不設外鍵：使用者刪除後紀錄仍須保留。
    }
}
