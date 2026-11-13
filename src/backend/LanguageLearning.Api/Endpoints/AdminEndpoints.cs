using LanguageLearning.Application.Admin;
using LanguageLearning.Application.Common.Paging;
using LanguageLearning.Application.Common.Security;

namespace LanguageLearning.Api.Endpoints;

public static class AdminEndpoints
{
    public static IEndpointRouteBuilder MapAdminEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/admin")
            .WithTags("Admin")
            .RequireAuthorization(Policies.AdminOnly);

        group.MapGet("/summary", async (IAdminService admin, CancellationToken ct) =>
                TypedResults.Ok(await admin.GetSummaryAsync(ct)))
            .Produces<AdminSummaryDto>();

        group.MapGet("/users", async ([AsParameters] AdminUserQuery query, IAdminService admin, CancellationToken ct) =>
                TypedResults.Ok(await admin.GetUsersAsync(query, ct)))
            .Produces<PagedResult<AdminUserDto>>();

        group.MapGet("/audit-logs", async ([AsParameters] AuditLogQuery query, IAdminService admin, CancellationToken ct) =>
                TypedResults.Ok(await admin.GetAuditLogsAsync(query, ct)))
            .Produces<PagedResult<AuditLogDto>>();

        return app;
    }
}
