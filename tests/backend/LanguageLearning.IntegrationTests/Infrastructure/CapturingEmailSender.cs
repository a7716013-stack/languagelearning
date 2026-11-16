using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using LanguageLearning.Application.Abstractions;
using LanguageLearning.Application.Common.Email;

namespace LanguageLearning.IntegrationTests.Infrastructure;

/// <summary>
/// 測試用寄件器：收集背景服務寄出的信件。
/// 信件由寄件匣背景服務非同步寄出，因此測試要用 Wait* 方法等待，而不是在請求後立即讀取。
/// </summary>
public sealed partial class CapturingEmailSender : IEmailSender
{
    public static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(20);

    private readonly ConcurrentQueue<EmailMessage> _messages = new();
    private readonly ConcurrentDictionary<string, int> _failuresRemaining = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, int> _failedAttempts = new(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyList<EmailMessage> Messages => _messages.ToList();

    public Task SendAsync(EmailMessage message, CancellationToken cancellationToken)
    {
        // 只影響指定收件者，避免干擾同一主機上並行的其他測試。
        if (_failuresRemaining.TryGetValue(message.To, out var remaining) && remaining > 0)
        {
            _failuresRemaining[message.To] = remaining - 1;
            _failedAttempts.AddOrUpdate(message.To, 1, (_, count) => count + 1);
            // 真實 SMTP 例外訊息可能含收件者地址；這裡刻意放入，用來驗證寄件匣與日誌會遮蔽。
            throw new InvalidOperationException($"模擬 SMTP 拒絕收件者 <{message.To}>。");
        }

        _messages.Enqueue(message);
        return Task.CompletedTask;
    }

    /// <summary>讓寄給指定收件者的接下來 <paramref name="times"/> 次寄送失敗。</summary>
    public void FailNextFor(string email, int times) => _failuresRemaining[email] = times;

    /// <summary>寄給指定收件者時已模擬失敗的次數。</summary>
    public int FailedAttemptsFor(string email) => _failedAttempts.GetValueOrDefault(email);

    public IReadOnlyList<EmailMessage> For(string email) =>
        _messages.Where(m => string.Equals(m.To, email, StringComparison.OrdinalIgnoreCase)).ToList();

    /// <summary>等到寄給該收件者的信件達到指定數量，回傳全部信件。</summary>
    public async Task<IReadOnlyList<EmailMessage>> WaitForCountAsync(string email, int count, TimeSpan? timeout = null)
    {
        var deadline = DateTime.UtcNow + (timeout ?? DefaultTimeout);
        while (true)
        {
            var messages = For(email);
            if (messages.Count >= count)
            {
                return messages;
            }

            if (DateTime.UtcNow >= deadline)
            {
                throw new TimeoutException($"等待逾時：測試帳號應收到 {count} 封信，實際 {messages.Count} 封。");
            }

            await Task.Delay(50, TestContext.Current.CancellationToken);
        }
    }

    /// <summary>等待指定條件在一段時間內都不成立（用來驗證「不會寄信」）。</summary>
    public async Task AssertNoneForAsync(string email, TimeSpan window)
    {
        var deadline = DateTime.UtcNow + window;
        while (DateTime.UtcNow < deadline)
        {
            Assert.Empty(For(email));
            await Task.Delay(100, TestContext.Current.CancellationToken);
        }

        Assert.Empty(For(email));
    }

    /// <summary>
    /// 等待寄給該收件者、包含指定路徑連結的第 <paramref name="occurrence"/> 封信（從 1 起算），回傳連結參數。
    /// </summary>
    public async Task<IReadOnlyDictionary<string, string>> WaitForLinkQueryAsync(
        string email,
        string path,
        int occurrence = 1,
        TimeSpan? timeout = null)
    {
        var deadline = DateTime.UtcNow + (timeout ?? DefaultTimeout);
        while (true)
        {
            var matching = For(email).Where(m => m.TextBody.Contains(path, StringComparison.Ordinal)).ToList();
            if (matching.Count >= occurrence)
            {
                return ParseLinkQuery(matching[occurrence - 1], path);
            }

            if (DateTime.UtcNow >= deadline)
            {
                throw new TimeoutException($"等待逾時：找不到寄給測試帳號、包含 {path} 的第 {occurrence} 封信。");
            }

            await Task.Delay(50, TestContext.Current.CancellationToken);
        }
    }

    private static Dictionary<string, string> ParseLinkQuery(EmailMessage message, string path)
    {
        var match = LinkPattern().Matches(message.TextBody)
            .Select(m => new Uri(m.Value))
            .First(uri => uri.AbsolutePath == path);

        return match.Query.TrimStart('?')
            .Split('&', StringSplitOptions.RemoveEmptyEntries)
            .Select(part => part.Split('=', 2))
            .ToDictionary(pair => Uri.UnescapeDataString(pair[0]), pair => Uri.UnescapeDataString(pair[1]));
    }

    [GeneratedRegex(@"https?://\S+")]
    private static partial Regex LinkPattern();
}
