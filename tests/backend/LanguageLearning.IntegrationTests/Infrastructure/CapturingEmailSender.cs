using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using LanguageLearning.Application.Abstractions;
using LanguageLearning.Application.Common.Email;

namespace LanguageLearning.IntegrationTests.Infrastructure;

public sealed partial class CapturingEmailSender : IEmailSender
{
    private readonly ConcurrentQueue<EmailMessage> _messages = new();

    public IReadOnlyList<EmailMessage> Messages => _messages.ToList();

    public Task SendAsync(EmailMessage message, CancellationToken cancellationToken)
    {
        _messages.Enqueue(message);
        return Task.CompletedTask;
    }

    public IReadOnlyList<EmailMessage> For(string email) =>
        _messages.Where(m => string.Equals(m.To, email, StringComparison.OrdinalIgnoreCase)).ToList();

    /// <summary>取出該收件者最新一封信中指定路徑的連結參數。</summary>
    public IReadOnlyDictionary<string, string> LatestLinkQuery(string email, string path)
    {
        var message = For(email).LastOrDefault(m => m.TextBody.Contains(path, StringComparison.Ordinal))
            ?? throw new InvalidOperationException($"找不到寄給測試帳號、包含 {path} 的信件。");

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
