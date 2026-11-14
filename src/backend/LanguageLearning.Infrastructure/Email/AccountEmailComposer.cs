using System.Text;
using LanguageLearning.Application.Abstractions;
using LanguageLearning.Application.Common.Email;
using LanguageLearning.Infrastructure.Identity;
using LanguageLearning.Infrastructure.Options;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;

namespace LanguageLearning.Infrastructure.Email;

/// <summary>
/// 寄出前才組成信件內容並產生 Token，因此 Token 不會保存在資料庫，有效期限也從實際寄出時開始計算。
/// 回傳 null 代表這封信已不需要寄出（例如使用者已刪除，或 Email 已驗證）。
/// </summary>
internal sealed class AccountEmailComposer(UserManager<ApplicationUser> userManager, IOptions<AppOptions> appOptions)
{
    public async Task<EmailMessage?> ComposeAsync(AccountEmailKind kind, Guid userId)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user?.Email is null)
        {
            return null;
        }

        switch (kind)
        {
            case AccountEmailKind.ConfirmEmail:
                if (user.EmailConfirmed)
                {
                    return null;
                }

                var confirmToken = await userManager.GenerateEmailConfirmationTokenAsync(user);
                return EmailTemplates.ConfirmEmail(
                    user.Email,
                    user.DisplayName,
                    BuildLink("/confirm-email", user.Id, confirmToken));

            case AccountEmailKind.AccountAlreadyExists:
                return EmailTemplates.AccountAlreadyExists(
                    user.Email,
                    user.DisplayName,
                    EmailTemplates.BuildLink(appOptions.Value.PublicBaseUrl, "/login", new Dictionary<string, string>()));

            case AccountEmailKind.ResetPassword:
                if (!user.EmailConfirmed)
                {
                    return null;
                }

                var resetToken = await userManager.GeneratePasswordResetTokenAsync(user);
                return EmailTemplates.ResetPassword(
                    user.Email,
                    user.DisplayName,
                    BuildLink("/reset-password", user.Id, resetToken));

            default:
                throw new ArgumentOutOfRangeException(nameof(kind), kind, "不支援的信件種類。");
        }
    }

    private Uri BuildLink(string path, Guid userId, string token) =>
        EmailTemplates.BuildLink(
            appOptions.Value.PublicBaseUrl,
            path,
            new Dictionary<string, string>
            {
                ["userId"] = userId.ToString(),
                ["token"] = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token)),
            });
}
