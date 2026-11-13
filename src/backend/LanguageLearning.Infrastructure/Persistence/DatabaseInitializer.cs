using LanguageLearning.Application.Abstractions;
using LanguageLearning.Application.Common.Auditing;
using LanguageLearning.Application.Common.Security;
using LanguageLearning.Domain.Auditing;
using LanguageLearning.Infrastructure.Identity;
using LanguageLearning.Infrastructure.Options;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LanguageLearning.Infrastructure.Persistence;

/// <summary>
/// 啟動時的資料庫初始化：（選擇性）套用 Migration，並建立設定中的管理者帳號。可重複執行。
/// 角色與考試類型等參考資料由 Migration（HasData）建立。
/// </summary>
public sealed class DatabaseInitializer(
    AppDbContext db,
    UserManager<ApplicationUser> userManager,
    IOptions<DatabaseOptions> databaseOptions,
    IOptions<SeedOptions> seedOptions,
    IAuditLogger auditLogger,
    TimeProvider timeProvider,
    ILogger<DatabaseInitializer> logger)
{
    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        if (databaseOptions.Value.ApplyMigrationsOnStartup)
        {
            logger.LogInformation("套用資料庫 Migration。");
            await db.Database.MigrateAsync(cancellationToken);
        }

        var pending = (await db.Database.GetPendingMigrationsAsync(cancellationToken)).ToList();
        if (pending.Count > 0)
        {
            logger.LogWarning(
                "資料庫仍有 {Count} 個 Migration 尚未套用，略過管理者帳號初始化。請執行 dotnet ef database update。",
                pending.Count);
            return;
        }

        await SeedAdminAsync(cancellationToken);
    }

    private async Task SeedAdminAsync(CancellationToken cancellationToken)
    {
        var options = seedOptions.Value;
        if (string.IsNullOrWhiteSpace(options.Email) || string.IsNullOrWhiteSpace(options.Password))
        {
            logger.LogInformation("未設定 Seed:Admin，略過管理者帳號初始化。");
            return;
        }

        var email = options.Email.Trim();
        var user = await userManager.FindByEmailAsync(email);
        if (user is null)
        {
            var now = timeProvider.GetUtcNow();
            user = new ApplicationUser
            {
                Id = Guid.CreateVersion7(now),
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                DisplayName = string.IsNullOrWhiteSpace(options.DisplayName) ? "系統管理員" : options.DisplayName.Trim(),
                CreatedAtUtc = now.UtcDateTime,
            };

            var created = await userManager.CreateAsync(user, options.Password);
            if (!created.Succeeded)
            {
                throw new InvalidOperationException(
                    "無法建立管理者帳號：" + string.Join("；", created.Errors.Select(e => e.Description)));
            }

            await auditLogger.WriteAsync(
                new AuditEntry(AuditActions.AdminSeeded, true, user.Id, "User", user.Id.ToString()),
                cancellationToken);
            logger.LogInformation("已建立管理者帳號。UserId={UserId}", user.Id);
        }

        if (!await userManager.IsInRoleAsync(user, Roles.Admin))
        {
            var added = await userManager.AddToRoleAsync(user, Roles.Admin);
            if (!added.Succeeded)
            {
                throw new InvalidOperationException("無法指派 Admin 角色，請確認資料庫 Migration 已套用。");
            }
        }
    }
}
