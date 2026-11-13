using LanguageLearning.Api.Common;
using LanguageLearning.Application.Common.Security;
using LanguageLearning.Infrastructure.Options;

namespace LanguageLearning.Api.Security;

public static class AuthenticationSetup
{
    /// <summary>
    /// 設定 Identity Cookie 與 Antiforgery Cookie。正式環境使用 __Host- 前綴並強制 Secure；
    /// 開發與測試環境（HTTP）使用 SameAsRequest。API 一律回傳 401 / 403，不轉址。
    /// </summary>
    public static IServiceCollection AddAppAuthentication(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        var auth = configuration.GetSection(AuthOptions.SectionName).Get<AuthOptions>() ?? new AuthOptions();
        var strict = environment.IsProduction();
        var securePolicy = strict ? CookieSecurePolicy.Always : CookieSecurePolicy.SameAsRequest;

        services.ConfigureApplicationCookie(options =>
        {
            options.Cookie.Name = strict ? "__Host-ll.auth" : "ll.auth";
            options.Cookie.HttpOnly = true;
            options.Cookie.SameSite = SameSiteMode.Strict;
            options.Cookie.SecurePolicy = securePolicy;
            options.Cookie.Path = "/";
            options.ExpireTimeSpan = TimeSpan.FromMinutes(auth.CookieExpirationMinutes);
            options.SlidingExpiration = true;
            options.Events.OnRedirectToLogin = context =>
                Problems.WriteAsync(context.HttpContext, StatusCodes.Status401Unauthorized, "not_authenticated", "請先登入。");
            options.Events.OnRedirectToAccessDenied = context =>
                Problems.WriteAsync(context.HttpContext, StatusCodes.Status403Forbidden, "forbidden", "您沒有權限執行此操作。");
        });

        services.AddAntiforgery(options =>
        {
            options.HeaderName = CsrfProtectionMiddleware.HeaderName;
            options.Cookie.Name = strict ? "__Host-ll.csrf" : "ll.csrf";
            options.Cookie.HttpOnly = true;
            options.Cookie.SameSite = SameSiteMode.Strict;
            options.Cookie.SecurePolicy = securePolicy;
            options.Cookie.Path = "/";
            options.SuppressXFrameOptionsHeader = true;
        });

        services.AddAuthorizationBuilder()
            .AddPolicy(Policies.AdminOnly, policy => policy.RequireAuthenticatedUser().RequireRole(Roles.Admin));

        return services;
    }
}
