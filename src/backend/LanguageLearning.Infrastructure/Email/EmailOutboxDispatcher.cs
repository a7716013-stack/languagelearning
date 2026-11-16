using LanguageLearning.Application.Abstractions;
using LanguageLearning.Application.Common.Auditing;
using LanguageLearning.Domain.Auditing;
using LanguageLearning.Infrastructure.Options;
using LanguageLearning.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LanguageLearning.Infrastructure.Email;

/// <summary>
/// 處理寄件匣中到期的信件。多個執行個體同時處理時，以 rowversion 搶占信件，同一封信只會被一個執行個體寄出。
/// </summary>
public sealed class EmailOutboxDispatcher(
    IServiceScopeFactory scopeFactory,
    IEmailSender emailSender,
    IOptions<EmailOptions> options,
    TimeProvider timeProvider,
    ILogger<EmailOutboxDispatcher> logger)
{
    private static readonly int[] RetryMultipliers = [1, 5, 15, 30];
    private const int MaxErrorLength = 500;

    /// <summary>處理一批到期的信件，回傳已處理的數量。</summary>
    public async Task<int> ProcessDueAsync(CancellationToken cancellationToken)
    {
        var settings = options.Value.Outbox;
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var staleBefore = now.AddSeconds(-settings.StaleProcessingSeconds);

        List<Guid> dueIds;
        await using (var scope = scopeFactory.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            dueIds = await db.EmailOutbox
                .AsNoTracking()
                .Where(m =>
                    (m.Status == EmailOutboxStatus.Pending && m.NextAttemptAtUtc <= now)
                    || (m.Status == EmailOutboxStatus.Processing && m.LastAttemptAtUtc < staleBefore))
                .OrderBy(m => m.NextAttemptAtUtc)
                .Select(m => m.Id)
                .Take(settings.BatchSize)
                .ToListAsync(cancellationToken);
        }

        var processed = 0;
        foreach (var id in dueIds)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (await ProcessOneAsync(id, settings, cancellationToken))
            {
                processed++;
            }
        }

        return processed;
    }

    private async Task<bool> ProcessOneAsync(Guid id, EmailOutboxOptions settings, CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var message = await db.EmailOutbox.SingleOrDefaultAsync(m => m.Id == id, cancellationToken);
        if (message is null || message.Status is EmailOutboxStatus.Sent or EmailOutboxStatus.Failed or EmailOutboxStatus.Skipped)
        {
            return false;
        }

        // 搶占：改為處理中。若另一個執行個體已先處理，rowversion 不符會丟出並行例外。
        var now = timeProvider.GetUtcNow().UtcDateTime;
        message.Status = EmailOutboxStatus.Processing;
        message.Attempts++;
        message.LastAttemptAtUtc = now;
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return false;
        }

        try
        {
            var composer = scope.ServiceProvider.GetRequiredService<AccountEmailComposer>();
            var email = await composer.ComposeAsync(message.Kind, message.UserId);
            if (email is null)
            {
                message.Status = EmailOutboxStatus.Skipped;
            }
            else
            {
                await emailSender.SendAsync(email, cancellationToken);
                message.Status = EmailOutboxStatus.Sent;
            }

            message.CompletedAtUtc = timeProvider.GetUtcNow().UtcDateTime;
            message.LastError = null;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            message.LastError = Truncate($"{ex.GetType().Name}: {AuditDetailsSanitizer.MaskEmail(ex.Message)}");
            if (message.Attempts >= settings.MaxAttempts)
            {
                message.Status = EmailOutboxStatus.Failed;
                message.CompletedAtUtc = timeProvider.GetUtcNow().UtcDateTime;
                // 不記錄例外本身：SMTP 例外訊息可能含收件者地址。
                logger.LogError(
                    "系統信件寄送失敗，已停止重試。OutboxId={OutboxId} Kind={Kind} Attempts={Attempts} Error={Error}",
                    message.Id,
                    message.Kind,
                    message.Attempts,
                    ex.GetType().Name);

                var audit = scope.ServiceProvider.GetRequiredService<IAuditLogger>();
                await audit.WriteAsync(
                    new AuditEntry(
                        AuditActions.EmailDeliveryFailed,
                        Succeeded: false,
                        ActorUserId: message.UserId,
                        EntityType: nameof(EmailOutboxMessage),
                        EntityId: message.Id.ToString(),
                        Details: new Dictionary<string, object?>
                        {
                            ["kind"] = message.Kind.ToString(),
                            ["attempts"] = message.Attempts,
                        }),
                    CancellationToken.None);
            }
            else
            {
                var multiplier = RetryMultipliers[Math.Min(message.Attempts, RetryMultipliers.Length) - 1];
                message.Status = EmailOutboxStatus.Pending;
                message.NextAttemptAtUtc = timeProvider.GetUtcNow().UtcDateTime
                    .AddSeconds(settings.RetryBaseDelaySeconds * multiplier);
                logger.LogWarning(
                    "系統信件寄送失敗，稍後重試。OutboxId={OutboxId} Kind={Kind} Attempts={Attempts} Error={Error}",
                    message.Id,
                    message.Kind,
                    message.Attempts,
                    ex.GetType().Name);
            }
        }

        // 結果一定要寫回，即使請求已取消。
        await db.SaveChangesAsync(CancellationToken.None);
        return true;
    }

    private static string Truncate(string value) =>
        value.Length <= MaxErrorLength ? value : value[..MaxErrorLength];
}
