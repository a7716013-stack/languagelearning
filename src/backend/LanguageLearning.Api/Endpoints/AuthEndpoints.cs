using LanguageLearning.Api.Common;
using LanguageLearning.Api.Security;
using LanguageLearning.Application.Auth;
using LanguageLearning.Application.Users;
using Microsoft.AspNetCore.Antiforgery;

namespace LanguageLearning.Api.Endpoints;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/auth").WithTags("Auth");

        group.MapGet("/csrf", (HttpContext httpContext, IAntiforgery antiforgery) =>
            {
                var tokens = antiforgery.GetAndStoreTokens(httpContext);
                httpContext.Response.Headers.CacheControl = "no-store";
                return TypedResults.Ok(new CsrfTokenResponse(tokens.RequestToken!));
            })
            .WithSummary("取得 CSRF Token（放在 X-XSRF-TOKEN Header）")
            .AllowAnonymous();

        group.MapPost("/register", async (RegisterRequest request, IAuthService auth, CancellationToken ct) =>
                (await auth.RegisterAsync(request, ct)).ToHttpResult(() =>
                    TypedResults.Accepted((string?)null, new MessageResponse("註冊申請已送出，請到信箱完成 Email 驗證。"))))
            .WithValidation<RegisterRequest>()
            .RequireRateLimiting(RateLimitPolicies.Auth)
            .Produces<MessageResponse>(StatusCodes.Status202Accepted)
            .WithSummary("註冊（為避免帳號列舉，一律回傳 202）")
            .AllowAnonymous();

        group.MapPost("/confirm-email", async (ConfirmEmailRequest request, IAuthService auth, CancellationToken ct) =>
                (await auth.ConfirmEmailAsync(request, ct)).ToHttpResult(() =>
                    TypedResults.Ok(new MessageResponse("Email 驗證完成，現在可以登入了。"))))
            .WithValidation<ConfirmEmailRequest>()
            .RequireRateLimiting(RateLimitPolicies.Auth)
            .Produces<MessageResponse>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .AllowAnonymous();

        group.MapPost("/resend-confirmation", async (ResendConfirmationRequest request, IAuthService auth, CancellationToken ct) =>
                (await auth.ResendConfirmationAsync(request, ct)).ToHttpResult(() =>
                    TypedResults.Accepted((string?)null, new MessageResponse("若此 Email 尚未驗證，我們已重新寄出驗證信。"))))
            .WithValidation<ResendConfirmationRequest>()
            .RequireRateLimiting(RateLimitPolicies.Auth)
            .Produces<MessageResponse>(StatusCodes.Status202Accepted)
            .AllowAnonymous();

        group.MapPost("/login", async (LoginRequest request, IAuthService auth, CancellationToken ct) =>
                (await auth.LoginAsync(request, ct)).ToHttpResult(user => TypedResults.Ok(user)))
            .WithValidation<LoginRequest>()
            .RequireRateLimiting(RateLimitPolicies.Auth)
            .Produces<CurrentUserDto>()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status423Locked)
            .AllowAnonymous();

        group.MapPost("/logout", async (IAuthService auth, CancellationToken ct) =>
                (await auth.LogoutAsync(ct)).ToHttpResult(() => TypedResults.NoContent()))
            .Produces(StatusCodes.Status204NoContent)
            .RequireAuthorization();

        group.MapPost("/forgot-password", async (ForgotPasswordRequest request, IAuthService auth, CancellationToken ct) =>
                (await auth.ForgotPasswordAsync(request, ct)).ToHttpResult(() =>
                    TypedResults.Accepted((string?)null, new MessageResponse("若此 Email 已註冊，我們已寄出重設密碼信。"))))
            .WithValidation<ForgotPasswordRequest>()
            .RequireRateLimiting(RateLimitPolicies.Auth)
            .Produces<MessageResponse>(StatusCodes.Status202Accepted)
            .AllowAnonymous();

        group.MapPost("/reset-password", async (ResetPasswordRequest request, IAuthService auth, CancellationToken ct) =>
                (await auth.ResetPasswordAsync(request, ct)).ToHttpResult(() =>
                    TypedResults.Ok(new MessageResponse("密碼已重設，請使用新密碼登入。"))))
            .WithValidation<ResetPasswordRequest>()
            .RequireRateLimiting(RateLimitPolicies.Auth)
            .Produces<MessageResponse>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .AllowAnonymous();

        group.MapPost("/change-password", async (ChangePasswordRequest request, IAuthService auth, CancellationToken ct) =>
                (await auth.ChangePasswordAsync(request, ct)).ToHttpResult(() =>
                    TypedResults.Ok(new MessageResponse("密碼已變更。"))))
            .WithValidation<ChangePasswordRequest>()
            .RequireRateLimiting(RateLimitPolicies.Auth)
            .Produces<MessageResponse>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .RequireAuthorization();

        return app;
    }
}
