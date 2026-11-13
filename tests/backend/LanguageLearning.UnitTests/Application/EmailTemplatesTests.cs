using LanguageLearning.Application.Common.Email;

namespace LanguageLearning.UnitTests.Application;

public sealed class EmailTemplatesTests
{
    [Fact]
    public void BuildLink_combines_base_url_path_and_escaped_query()
    {
        var link = EmailTemplates.BuildLink(
            "http://localhost:5173/",
            "/confirm-email",
            new Dictionary<string, string> { ["userId"] = "abc", ["token"] = "a+b/c=" });

        Assert.Equal("http://localhost:5173/confirm-email?userId=abc&token=a%2Bb%2Fc%3D", link.AbsoluteUri);
    }

    [Fact]
    public void BuildLink_keeps_base_path()
    {
        var link = EmailTemplates.BuildLink("https://example.com/app", "/login", new Dictionary<string, string>());

        Assert.Equal("https://example.com/app/login", link.AbsoluteUri);
    }

    [Fact]
    public void ConfirmEmail_contains_link_in_html_and_text()
    {
        var link = new Uri("http://localhost:5173/confirm-email?userId=1&token=t");

        var message = EmailTemplates.ConfirmEmail("user@example.com", "小明", link);

        Assert.Equal("user@example.com", message.To);
        Assert.Contains("驗證", message.Subject, StringComparison.Ordinal);
        Assert.Contains(link.AbsoluteUri, message.TextBody, StringComparison.Ordinal);
        Assert.Contains("confirm-email?userId=1&amp;token=t", message.HtmlBody, StringComparison.Ordinal);
    }

    [Fact]
    public void Templates_html_encode_display_name()
    {
        var message = EmailTemplates.ResetPassword(
            "user@example.com",
            "<script>alert(1)</script>",
            new Uri("http://localhost:5173/reset-password"));

        Assert.DoesNotContain("<script>", message.HtmlBody, StringComparison.Ordinal);
        Assert.Contains("&lt;script&gt;", message.HtmlBody, StringComparison.Ordinal);
    }
}
