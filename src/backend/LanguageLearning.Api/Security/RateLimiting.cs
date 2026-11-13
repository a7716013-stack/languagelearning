using System.Globalization;
using System.Security.Claims;
using System.Threading.RateLimiting;
using LanguageLearning.Api.Common;
using Microsoft.AspNetCore.RateLimiting;

namespace LanguageLearning.Api.Security;

public static class RateLimitPolicies
{
    /// <summary>登入、註冊、忘記密碼等驗證相關端點（依 IP 分區）。</summary>
    public const string Auth = "auth";
}

public sealed class RateLimitingOptions
{
    public const string SectionName = "RateLimiting";

    public WindowLimit Auth { get; set; } = new() { PermitLimit = 10, WindowSeconds = 60 };

    public WindowLimit Api { get; set; } = new() { PermitLimit = 300, WindowSeconds = 60 };

    public sealed class WindowLimit
    {
        public int PermitLimit { get; set; }

        public int WindowSeconds { get; set; }
    }
}

public static class RateLimitingSetup
{
    public static IServiceCollection AddAppRateLimiting(this IServiceCollection services, IConfiguration configuration)
    {
        var settings = configuration.GetSection(RateLimitingOptions.SectionName).Get<RateLimitingOptions>()
            ?? new RateLimitingOptions();

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.OnRejected = async (context, _) =>
            {
                if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                {
                    context.HttpContext.Response.Headers.RetryAfter =
                        ((int)Math.Ceiling(retryAfter.TotalSeconds)).ToString(CultureInfo.InvariantCulture);
                }

                await Problems.WriteAsync(
                    context.HttpContext,
                    StatusCodes.Status429TooManyRequests,
                    "rate_limited",
                    "請求太頻繁，請稍後再試。");
            };

            options.AddPolicy(RateLimitPolicies.Auth, httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: ClientIp(httpContext),
                    factory: _ => CreateWindow(settings.Auth)));

            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
            {
                if (!httpContext.Request.Path.StartsWithSegments("/api"))
                {
                    return RateLimitPartition.GetNoLimiter("non-api");
                }

                var userId = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
                var partitionKey = userId is not null ? $"user:{userId}" : $"ip:{ClientIp(httpContext)}";
                return RateLimitPartition.GetFixedWindowLimiter(partitionKey, _ => CreateWindow(settings.Api));
            });
        });

        return services;
    }

    private static string ClientIp(HttpContext httpContext) =>
        httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

    private static FixedWindowRateLimiterOptions CreateWindow(RateLimitingOptions.WindowLimit limit) =>
        new()
        {
            PermitLimit = Math.Max(1, limit.PermitLimit),
            Window = TimeSpan.FromSeconds(Math.Max(1, limit.WindowSeconds)),
            QueueLimit = 0,
            AutoReplenishment = true,
        };
}
