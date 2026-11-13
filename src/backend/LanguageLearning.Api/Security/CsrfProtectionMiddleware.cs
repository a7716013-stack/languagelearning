using LanguageLearning.Api.Common;
using Microsoft.AspNetCore.Antiforgery;

namespace LanguageLearning.Api.Security;

/// <summary>
/// 對所有 /api 的狀態變更請求（POST、PUT、PATCH、DELETE）驗證 Antiforgery Token（Header：X-XSRF-TOKEN）。
/// 必須放在 UseAuthentication 之後，因為 Token 綁定使用者身分。
/// </summary>
public sealed class CsrfProtectionMiddleware(RequestDelegate next, IAntiforgery antiforgery)
{
    public const string HeaderName = "X-XSRF-TOKEN";

    public async Task InvokeAsync(HttpContext context)
    {
        if (RequiresValidation(context.Request))
        {
            try
            {
                await antiforgery.ValidateRequestAsync(context);
            }
            catch (AntiforgeryValidationException)
            {
                await Problems.WriteAsync(
                    context,
                    StatusCodes.Status400BadRequest,
                    "csrf_invalid",
                    "安全驗證已失效，請重新整理頁面後再試。");
                return;
            }
        }

        await next(context);
    }

    private static bool RequiresValidation(HttpRequest request) =>
        request.Path.StartsWithSegments("/api")
        && !HttpMethods.IsGet(request.Method)
        && !HttpMethods.IsHead(request.Method)
        && !HttpMethods.IsOptions(request.Method)
        && !HttpMethods.IsTrace(request.Method);
}
