using LanguageLearning.Domain.Goals;
using LanguageLearning.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LanguageLearning.Infrastructure.Persistence.Configurations;

internal sealed class UserExamGoalConfiguration : IEntityTypeConfiguration<UserExamGoal>
{
    public void Configure(EntityTypeBuilder<UserExamGoal> builder)
    {
        builder.ToTable("UserExamGoals");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.TargetScore).HasPrecision(6, 1);
        builder.Property(x => x.RowVersion).IsRowVersion();
        builder.HasIndex(x => new { x.UserId, x.ExamTypeId }).IsUnique();

        builder.HasOne(x => x.ExamType)
            .WithMany()
            .HasForeignKey(x => x.ExamTypeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
