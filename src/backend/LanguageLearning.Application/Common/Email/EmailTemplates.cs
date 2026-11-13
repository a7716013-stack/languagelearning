using System.Net;

namespace LanguageLearning.Application.Common.Email;

/// <summary>繁體中文系統信件範本。</summary>
public static class EmailTemplates
{
    private const string ProductName = "LanguageLearning";

    public static EmailMessage ConfirmEmail(string to, string displayName, Uri link) =>
        Build(
            to,
            $"[{ProductName}] 請驗證您的 Email",
            displayName,
            "感謝您註冊 LanguageLearning。請點擊下方按鈕完成 Email 驗證，驗證後即可登入。",
            "驗證 Email",
            link,
            "如果您沒有註冊帳號，請忽略此信件。");

    public static EmailMessage AccountAlreadyExists(string to, string displayName, Uri loginLink) =>
        Build(
            to,
            $"[{ProductName}] 此 Email 已註冊",
            displayName,
            "有人使用您的 Email 嘗試註冊新帳號，但此 Email 已經有帳號了。您可以直接登入；若忘記密碼，可在登入頁使用「忘記密碼」。",
            "前往登入",
            loginLink,
            "如果這不是您本人的操作，請忽略此信件，您的帳號不會有任何變更。");

    public static EmailMessage ResetPassword(string to, string displayName, Uri link) =>
        Build(
            to,
            $"[{ProductName}] 重設密碼",
            displayName,
            "我們收到重設密碼的請求。請點擊下方按鈕設定新密碼，連結將在一段時間後失效。",
            "重設密碼",
            link,
            "如果您沒有申請重設密碼，請忽略此信件，您的密碼不會被變更。");

    public static Uri BuildLink(string publicBaseUrl, string path, IReadOnlyDictionary<string, string> query)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(publicBaseUrl);
        var baseUri = new Uri(publicBaseUrl.TrimEnd('/') + "/", UriKind.Absolute);
        var queryString = string.Join(
            "&",
            query.Select(pair => $"{Uri.EscapeDataString(pair.Key)}={Uri.EscapeDataString(pair.Value)}"));
        var relative = path.TrimStart('/') + (queryString.Length > 0 ? "?" + queryString : string.Empty);
        return new Uri(baseUri, relative);
    }

    private static EmailMessage Build(
        string to,
        string subject,
        string displayName,
        string intro,
        string actionText,
        Uri link,
        string footer)
    {
        var safeName = WebUtility.HtmlEncode(string.IsNullOrWhiteSpace(displayName) ? "您好" : displayName);
        var safeLink = WebUtility.HtmlEncode(link.AbsoluteUri);

        var html = $"""
            <!DOCTYPE html>
            <html lang="zh-Hant-TW">
            <body style="margin:0;padding:24px;background:#f5f5ff;font-family:'Noto Sans TC','PingFang TC','Microsoft JhengHei',sans-serif;color:#1e1b4b;">
              <div style="max-width:560px;margin:0 auto;background:#ffffff;border-radius:12px;padding:32px;">
                <h1 style="margin:0 0 16px;font-size:20px;color:#4338ca;">{ProductName}</h1>
                <p style="margin:0 0 12px;">{safeName}，您好：</p>
                <p style="margin:0 0 24px;line-height:1.7;">{WebUtility.HtmlEncode(intro)}</p>
                <p style="margin:0 0 24px;">
                  <a href="{safeLink}" style="display:inline-block;background:#4f46e5;color:#ffffff;text-decoration:none;padding:12px 20px;border-radius:8px;">{WebUtility.HtmlEncode(actionText)}</a>
                </p>
                <p style="margin:0 0 8px;font-size:13px;color:#6b7280;">若按鈕無法使用，請複製以下網址到瀏覽器開啟：</p>
                <p style="margin:0 0 24px;font-size:13px;word-break:break-all;"><a href="{safeLink}">{safeLink}</a></p>
                <p style="margin:0;font-size:13px;color:#6b7280;">{WebUtility.HtmlEncode(footer)}</p>
              </div>
            </body>
            </html>
            """;

        var text = $"""
            {(string.IsNullOrWhiteSpace(displayName) ? "您好" : displayName)}，您好：

            {intro}

            {actionText}：{link.AbsoluteUri}

            {footer}

            — {ProductName}
            """;

        return new EmailMessage(to, subject, html, text);
    }
}
