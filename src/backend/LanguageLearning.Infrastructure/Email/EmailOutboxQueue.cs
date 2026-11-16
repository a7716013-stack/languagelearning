using LanguageLearning.Application.Abstractions;
using LanguageLearning.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LanguageLearning.Infrastructure.Email;

internal sealed class EmailOutboxQueue(AppDbContext db, EmailOutboxSignal signal, TimeProvider timeProvider)
    : IAccountEmailQueue
{
    public async Task EnqueueAsync(AccountEmailKind kind, Guid userId, CancellationToken cancellationToken)
    {
        // 同一使用者、同一種類已有尚未寄出的信件時，不重複排入（寄出時才產生 Token，內容一樣會是最新的）。
        var alreadyQueued = await db.EmailOutbox.AnyAsync(
            m => m.UserId == userId
                && m.Kind == kind
                && (m.Status == EmailOutboxStatus.Pending || m.Status == EmailOutboxStatus.Processing),
            cancellationToken);

        if (!alreadyQueued)
        {
            var now = timeProvider.GetUtcNow();
            db.EmailOutbox.Add(new EmailOutboxMessage
            {
                Id = Guid.CreateVersion7(now),
                Kind = kind,
                UserId = userId,
                Status = EmailOutboxStatus.Pending,
                CreatedAtUtc = now.UtcDateTime,
                NextAttemptAtUtc = now.UtcDateTime,
            });
            await db.SaveChangesAsync(cancellationToken);
        }

        signal.Notify();
    }
}
