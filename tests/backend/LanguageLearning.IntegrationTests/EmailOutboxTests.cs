using System.Net;
using LanguageLearning.Application.Abstractions;
using LanguageLearning.Domain.Auditing;
using LanguageLearning.Infrastructure.Email;
using LanguageLearning.Infrastructure.Persistence;
using LanguageLearning.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace LanguageLearning.IntegrationTests;

/// <summary>寄件匣重試較慢、最多嘗試 2 次，方便觀察「排隊中」與「重試用盡」的狀態。</summary>
public sealed class EmailOutboxApiFixture : ApiFixture
{
    protected override IReadOnlyDictionary<string, string?> Overrides { get; } = new Dictionary<string, string?>
    {
        ["Email:Outbox:MaxAttempts"] = "2",
        ["Email:Outbox:RetryBaseDelaySeconds"] = "3",
    };
}

public sealed class EmailOutboxTests(EmailOutboxApiFixture fixture) : IClassFixture<EmailOutboxApiFixture>
{
    private static readonly TimeSpan RetryTimeout = TimeSpan.FromSeconds(30);

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private ApiFactory Factory => fixture.Factory;

    [Fact]
    public async Task Register_succeeds_while_smtp_fails_and_email_is_retried()
    {
        var email = TestUsers.NewEmail();
        Factory.Emails.FailNextFor(email, 1);
        using var client = Factory.CreateTestClient();

        var register = await client.PostAsync(
            "/api/auth/register",
            new { email, password = TestUsers.Password, displayName = "重試" });

        // 寄信失敗不影響註冊請求。
        await TestClient.EnsureStatusAsync(register, HttpStatusCode.Accepted);

        var query = await Factory.Emails.WaitForLinkQueryAsync(email, "/confirm-email", timeout: RetryTimeout);
        Assert.Equal(1, Factory.Emails.FailedAttemptsFor(email));

        // 重試時才產生的 Token 仍然有效。
        var confirm = await client.PostAsync(
            "/api/auth/confirm-email",
            new { userId = query["userId"], token = query["token"] });
        await TestClient.EnsureStatusAsync(confirm, HttpStatusCode.OK);

        var message = await WaitForOutboxAsync(email, AccountEmailKind.ConfirmEmail, EmailOutboxStatus.Sent);
        Assert.Equal(2, message.Attempts);
        Assert.Null(message.LastError);
        Assert.NotNull(message.CompletedAtUtc);
    }

    [Fact]
    public async Task Outbox_stores_no_address_token_or_link()
    {
        // 寄件匣資料表的欄位固定，不得新增可保存收件者、內容、連結或 Token 的欄位。
        await using (var scope = Factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var columns = db.Model.FindEntityType(typeof(EmailOutboxMessage))!
                .GetProperties()
                .Select(p => p.Name)
                .Order(StringComparer.Ordinal)
                .ToArray();

            Assert.Equal(
                new[]
                {
                    "Attempts", "CompletedAtUtc", "CreatedAtUtc", "Id", "Kind", "LastAttemptAtUtc",
                    "LastError", "NextAttemptAtUtc", "RowVersion", "Status", "UserId",
                },
                columns);
        }

        // 寄送失敗時，錯誤訊息裡的收件者地址會被遮蔽，日誌也不含收件者地址。
        var email = TestUsers.NewEmail();
        Factory.Emails.FailNextFor(email, 1);
        using var client = Factory.CreateTestClient();
        await client.PostAsync("/api/auth/register", new { email, password = TestUsers.Password, displayName = "遮蔽" });

        var pending = await WaitForOutboxAsync(
            email,
            AccountEmailKind.ConfirmEmail,
            EmailOutboxStatus.Pending,
            m => m.Attempts == 1);
        Assert.NotNull(pending.LastError);
        Assert.DoesNotContain(email, pending.LastError, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("***@", pending.LastError, StringComparison.Ordinal);

        var query = await Factory.Emails.WaitForLinkQueryAsync(email, "/confirm-email", timeout: RetryTimeout);
        var sent = await WaitForOutboxAsync(email, AccountEmailKind.ConfirmEmail, EmailOutboxStatus.Sent);
        Assert.Null(sent.LastError);

        var logs = string.Join('\n', Factory.Logs.Entries);
        Assert.DoesNotContain(email, logs, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(query["token"], logs, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Repeated_requests_while_email_is_queued_do_not_send_duplicates()
    {
        var email = TestUsers.NewEmail();
        Factory.Emails.FailNextFor(email, 1);
        using var client = Factory.CreateTestClient();
        await client.PostAsync("/api/auth/register", new { email, password = TestUsers.Password, displayName = "重複" });

        // 第一次寄送失敗、等待重試時，再要求重寄驗證信。
        await WaitForOutboxAsync(email, AccountEmailKind.ConfirmEmail, EmailOutboxStatus.Pending, m => m.Attempts == 1);
        var resend = await client.PostAsync("/api/auth/resend-confirmation", new { email });
        await TestClient.EnsureStatusAsync(resend, HttpStatusCode.Accepted);

        await Factory.Emails.WaitForLinkQueryAsync(email, "/confirm-email", timeout: RetryTimeout);
        await WaitForOutboxAsync(email, AccountEmailKind.ConfirmEmail, EmailOutboxStatus.Sent);

        // 等過一次輪詢，確認沒有第二封。
        await Task.Delay(TimeSpan.FromSeconds(2), Ct);
        Assert.Single(Factory.Emails.For(email));
        Assert.Single(await OutboxForAsync(email));
    }

    [Fact]
    public async Task Exhausted_retries_mark_email_failed_and_write_audit_log()
    {
        var email = TestUsers.NewEmail();
        Factory.Emails.FailNextFor(email, 100);
        using var client = Factory.CreateTestClient();
        await client.PostAsync("/api/auth/register", new { email, password = TestUsers.Password, displayName = "失敗" });

        var failed = await WaitForOutboxAsync(email, AccountEmailKind.ConfirmEmail, EmailOutboxStatus.Failed);
        Assert.Equal(2, failed.Attempts);
        Assert.Equal(2, Factory.Emails.FailedAttemptsFor(email));
        Assert.Empty(Factory.Emails.For(email));

        await using var scope = Factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var audit = await db.AuditLogs.AsNoTracking()
            .Where(a => a.Action == AuditActions.EmailDeliveryFailed && a.EntityId == failed.Id.ToString())
            .ToListAsync(Ct);
        var entry = Assert.Single(audit);
        Assert.False(entry.Succeeded);
        Assert.DoesNotContain(email, entry.Details ?? string.Empty, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Forgot_password_for_unknown_email_queues_nothing()
    {
        var before = await CountOutboxAsync();
        using var client = Factory.CreateTestClient();

        var response = await client.PostAsync("/api/auth/forgot-password", new { email = TestUsers.NewEmail() });

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        Assert.Equal(before, await CountOutboxAsync());
    }

    private async Task<int> CountOutboxAsync()
    {
        await using var scope = Factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return await db.EmailOutbox.CountAsync(Ct);
    }

    private async Task<List<EmailOutboxMessage>> OutboxForAsync(string email)
    {
        await using var scope = Factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var normalized = email.ToUpperInvariant();
        var userId = await db.Users.Where(u => u.NormalizedEmail == normalized).Select(u => u.Id).SingleAsync(Ct);
        return await db.EmailOutbox.AsNoTracking().Where(m => m.UserId == userId).ToListAsync(Ct);
    }

    private async Task<EmailOutboxMessage> WaitForOutboxAsync(
        string email,
        AccountEmailKind kind,
        EmailOutboxStatus status,
        Func<EmailOutboxMessage, bool>? predicate = null)
    {
        var deadline = DateTime.UtcNow + RetryTimeout;
        while (true)
        {
            var messages = await OutboxForAsync(email);
            var match = messages.FirstOrDefault(m => m.Kind == kind && m.Status == status && (predicate?.Invoke(m) ?? true));
            if (match is not null)
            {
                return match;
            }

            if (DateTime.UtcNow >= deadline)
            {
                var states = string.Join(", ", messages.Select(m => $"{m.Kind}/{m.Status}/{m.Attempts}"));
                throw new TimeoutException($"等待逾時：寄件匣沒有 {kind}/{status} 的信件。目前：{states}");
            }

            await Task.Delay(100, Ct);
        }
    }
}
