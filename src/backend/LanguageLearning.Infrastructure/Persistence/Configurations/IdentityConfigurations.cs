using LanguageLearning.Application.Common.Security;
using LanguageLearning.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LanguageLearning.Infrastructure.Persistence.Configurations;

internal sealed class ApplicationUserConfiguration : IEntityTypeConfiguration<ApplicationUser>
{
    public void Configure(EntityTypeBuilder<ApplicationUser> builder)
    {
        builder.Property(x => x.DisplayName).HasMaxLength(50).IsRequired();
        builder.Property(x => x.TimeZoneId).HasMaxLength(64).IsRequired();
        builder.Property(x => x.UiTheme).HasMaxLength(10).IsRequired();
        builder.HasIndex(x => x.CreatedAtUtc);
    }
}

internal sealed class ApplicationRoleConfiguration : IEntityTypeConfiguration<ApplicationRole>
{
    public void Configure(EntityTypeBuilder<ApplicationRole> builder)
    {
        builder.HasData(
            new
            {
                Id = SeedData.AdminRoleId,
                Name = Roles.Admin,
                NormalizedName = Roles.Admin.ToUpperInvariant(),
                ConcurrencyStamp = SeedData.AdminRoleConcurrencyStamp,
            },
            new
            {
                Id = SeedData.StudentRoleId,
                Name = Roles.Student,
                NormalizedName = Roles.Student.ToUpperInvariant(),
                ConcurrencyStamp = SeedData.StudentRoleConcurrencyStamp,
            });
    }
}
