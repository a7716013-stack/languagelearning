using System.Text.Json;
using LanguageLearning.Application.Common.Auditing;

namespace LanguageLearning.UnitTests.Application;

public sealed class AuditDetailsSanitizerTests
{
    [Fact]
    public void Serialize_returns_null_for_empty_details()
    {
        Assert.Null(AuditDetailsSanitizer.Serialize(null));
        Assert.Null(AuditDetailsSanitizer.Serialize(new Dictionary<string, object?>()));
    }

    [Theory]
    [InlineData("password")]
    [InlineData("newPassword")]
    [InlineData("resetToken")]
    [InlineData("X-XSRF-TOKEN")]
    [InlineData("apiKey")]
    [InlineData("Cookie")]
    [InlineData("securityStamp")]
    public void Serialize_redacts_sensitive_keys(string key)
    {
        var json = AuditDetailsSanitizer.Serialize(new Dictionary<string, object?> { [key] = "super-secret-value" });

        Assert.NotNull(json);
        Assert.DoesNotContain("super-secret-value", json, StringComparison.Ordinal);
        Assert.Contains(AuditDetailsSanitizer.Redacted, json, StringComparison.Ordinal);
    }

    [Fact]
    public void Serialize_masks_email_addresses_in_values()
    {
        var json = AuditDetailsSanitizer.Serialize(new Dictionary<string, object?> { ["note"] = "user alice@example.com" });

        Assert.NotNull(json);
        Assert.DoesNotContain("alice@example.com", json, StringComparison.Ordinal);
        Assert.Contains("a***@example.com", json, StringComparison.Ordinal);
    }

    [Fact]
    public void Serialize_keeps_non_sensitive_values()
    {
        var json = AuditDetailsSanitizer.Serialize(new Dictionary<string, object?>
        {
            ["examType"] = "TOEIC",
            ["targetScore"] = 850m,
        });

        using var document = JsonDocument.Parse(json!);
        Assert.Equal("TOEIC", document.RootElement.GetProperty("examType").GetString());
        Assert.Equal(850m, document.RootElement.GetProperty("targetScore").GetDecimal());
    }

    [Fact]
    public void Serialize_truncates_long_details()
    {
        var json = AuditDetailsSanitizer.Serialize(new Dictionary<string, object?> { ["note"] = new string('x', 5000) });

        Assert.NotNull(json);
        Assert.True(json.Length <= AuditDetailsSanitizer.MaxLength);
    }
}
