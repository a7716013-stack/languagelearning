using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Text.Unicode;

namespace LanguageLearning.Application.Common.Auditing;

/// <summary>
/// 將稽核細節轉為 JSON，並遮蔽敏感欄位（密碼、Token、Cookie、Secret 等）與 Email。
/// </summary>
public static partial class AuditDetailsSanitizer
{
    public const string Redacted = "[已遮蔽]";
    public const int MaxLength = 2000;

    // 保留中文可讀性；HTML 敏感字元（< > & 等）仍會被跳脫。
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        Encoder = JavaScriptEncoder.Create(UnicodeRanges.All),
    };

    private static readonly string[] SensitiveKeyFragments =
    [
        "password", "passwd", "token", "secret", "cookie", "authorization", "apikey", "api_key", "otp", "verificationcode", "stamp",
    ];

    public static string? Serialize(IReadOnlyDictionary<string, object?>? details)
    {
        if (details is null || details.Count == 0)
        {
            return null;
        }

        var sanitized = new SortedDictionary<string, object?>(StringComparer.Ordinal);
        foreach (var (key, value) in details)
        {
            sanitized[key] = IsSensitiveKey(key) ? Redacted : SanitizeValue(value);
        }

        var json = JsonSerializer.Serialize(sanitized, SerializerOptions);
        return json.Length <= MaxLength ? json : json[..MaxLength];
    }

    public static bool IsSensitiveKey(string key)
    {
        var normalized = key.Replace("-", string.Empty, StringComparison.Ordinal).ToLowerInvariant();
        return SensitiveKeyFragments.Any(fragment => normalized.Contains(fragment, StringComparison.Ordinal));
    }

    /// <summary>遮蔽 Email，只保留第一個字元與網域，例如 a***@example.com。</summary>
    public static string MaskEmail(string value) =>
        EmailPattern().Replace(value, match =>
        {
            var local = match.Groups["local"].Value;
            return $"{local[0]}***@{match.Groups["domain"].Value}";
        });

    private static object? SanitizeValue(object? value) => value switch
    {
        null => null,
        string text => MaskEmail(text),
        bool or int or long or decimal or double or Guid or DateTime or DateOnly => value,
        _ => MaskEmail(value.ToString() ?? string.Empty),
    };

    [GeneratedRegex(@"(?<local>[A-Za-z0-9._%+\-]+)@(?<domain>[A-Za-z0-9.\-]+\.[A-Za-z]{2,})", RegexOptions.CultureInvariant)]
    private static partial Regex EmailPattern();
}
