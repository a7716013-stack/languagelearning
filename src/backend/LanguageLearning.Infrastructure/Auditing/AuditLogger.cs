using LanguageLearning.Application.Abstractions;
using LanguageLearning.Application.Common.Auditing;
using LanguageLearning.Domain.Auditing;
using LanguageLearning.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace LanguageLearning.Infrastructure.Auditing;

/// <summary>
/// 以獨立的 DbContext 寫入稽核紀錄，避免與主要流程的變更追蹤互相影響；寫入失敗只記錄錯誤，不中斷請求。
/// </summary>
internal sealed class AuditLogger(
    IServiceScopeFactory scopeFactory,
    ICurrentUser currentUser,
    TimeProvider timeProvider,
    ILogger<AuditLogger> logger) : IAuditLogger
{
    public async Task WriteAsync(AuditEntry entry, CancellationToken cancellationToken)
    {
        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.AuditLogs.Add(new AuditLog
            {
                OccurredAtUtc = timeProvider.GetUtcNow().UtcDateTime,
                ActorUserId = entry.ActorUserId ?? currentUser.UserId,
                Action = entry.Action,
                EntityType = entry.EntityType,
                EntityId = entry.EntityId,
                Succeeded = entry.Succeeded,
                IpAddress = currentUser.IpAddress,
                Details = AuditDetailsSanitizer.Serialize(entry.Details),
            });

            // 稽核紀錄即使請求已取消也要寫入。
            await db.SaveChangesAsync(CancellationToken.None);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "寫入稽核紀錄失敗。Action={Action}", entry.Action);
        }
    }
}
