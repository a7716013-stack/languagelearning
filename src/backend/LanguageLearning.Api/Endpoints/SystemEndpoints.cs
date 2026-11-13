using LanguageLearning.Api.Common;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;

namespace LanguageLearning.Api.Endpoints;

public static class SystemEndpoints
{
    public const string ReadyTag = "ready";

    public static IEndpointRouteBuilder MapSystemEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/version", (IHostEnvironment environment) =>
                TypedResults.Ok(new VersionResponse(AppVersion.Version, environment.EnvironmentName, AppVersion.Commit)))
            .WithTags("System")
            .AllowAnonymous();

        app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false })
            .AllowAnonymous();
        app.MapHealthChecks("/health/ready", new HealthCheckOptions
            {
                Predicate = check => check.Tags.Contains(ReadyTag),
            })
            .AllowAnonymous();

        return app;
    }
}
