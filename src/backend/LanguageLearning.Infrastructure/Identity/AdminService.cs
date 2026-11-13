using LanguageLearning.Application.Admin;
using LanguageLearning.Application.Common.Paging;
using LanguageLearning.Domain.Auditing;
using LanguageLearning.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LanguageLearning.Infrastructure.Identity;

internal sealed class AdminService(AppDbContext db, TimeProvider timeProvider) : IAdminService
{
    public async Task<AdminSummaryDto> GetSummaryAsync(CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var sevenDaysAgo = now.AddDays(-7);
        var oneDayAgo = now.AddHours(-24);

        var totalUsers = await db.Users.CountAsync(cancellationToken);
        var confirmedUsers = await db.Users.CountAsync(x => x.EmailConfirmed, cancellationToken);
        var adminCount = await db.UserRoles.CountAsync(x => x.RoleId == SeedData.AdminRoleId, cancellationToken);
        var newUsers = await db.Users.CountAsync(x => x.CreatedAtUtc >= sevenDaysAgo, cancellationToken);
        var successfulLogins = await db.AuditLogs.CountAsync(
            x => x.Action == AuditActions.LoginSucceeded && x.OccurredAtUtc >= oneDayAgo,
            cancellationToken);
        var failedLogins = await db.AuditLogs.CountAsync(
            x => x.Action == AuditActions.LoginFailed && x.OccurredAtUtc >= oneDayAgo,
            cancellationToken);

        return new AdminSummaryDto(totalUsers, confirmedUsers, adminCount, newUsers, successfulLogins, failedLogins);
    }

    public async Task<PagedResult<AdminUserDto>> GetUsersAsync(
        AdminUserQuery query,
        CancellationToken cancellationToken)
    {
        var (page, pageSize) = Paging.Normalize(query.Page, query.PageSize);
        var users = db.Users.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim();
            users = users.Where(x => x.Email!.Contains(term) || x.DisplayName.Contains(term));
        }

        var totalCount = await users.CountAsync(cancellationToken);
        var pageItems = await users
            .OrderByDescending(x => x.CreatedAtUtc)
            .ThenBy(x => x.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new
            {
                x.Id,
                x.Email,
                x.DisplayName,
                x.EmailConfirmed,
                x.LockoutEnd,
                x.CreatedAtUtc,
                x.LastLoginAtUtc,
            })
            .ToListAsync(cancellationToken);

        var ids = pageItems.Select(x => x.Id).ToList();
        var roles = await (
                from userRole in db.UserRoles
                join role in db.Roles on userRole.RoleId equals role.Id
                where ids.Contains(userRole.UserId)
                select new { userRole.UserId, role.Name })
            .ToListAsync(cancellationToken);
        var rolesByUser = roles
            .GroupBy(x => x.UserId)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<string>)g.Select(x => x.Name ?? string.Empty).Order().ToList());

        var now = timeProvider.GetUtcNow();
        var items = pageItems
            .Select(x => new AdminUserDto(
                x.Id,
                x.Email ?? string.Empty,
                x.DisplayName,
                rolesByUser.GetValueOrDefault(x.Id) ?? [],
                x.EmailConfirmed,
                x.LockoutEnd is { } lockoutEnd && lockoutEnd > now,
                x.CreatedAtUtc,
                x.LastLoginAtUtc))
            .ToList();

        return new PagedResult<AdminUserDto>(items, page, pageSize, totalCount);
    }

    public async Task<PagedResult<AuditLogDto>> GetAuditLogsAsync(
        AuditLogQuery query,
        CancellationToken cancellationToken)
    {
        var (page, pageSize) = Paging.Normalize(query.Page, query.PageSize);
        var logs = db.AuditLogs.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.Action))
        {
            var action = query.Action.Trim();
            logs = logs.Where(x => x.Action.StartsWith(action));
        }

        if (query.FromUtc is { } from)
        {
            var fromUtc = from.ToUniversalTime();
            logs = logs.Where(x => x.OccurredAtUtc >= fromUtc);
        }

        if (query.ToUtc is { } to)
        {
            var toUtc = to.ToUniversalTime();
            logs = logs.Where(x => x.OccurredAtUtc <= toUtc);
        }

        var totalCount = await logs.CountAsync(cancellationToken);
        var items = await (
                from log in logs.OrderByDescending(x => x.Id).Skip((page - 1) * pageSize).Take(pageSize)
                join user in db.Users on log.ActorUserId equals (Guid?)user.Id into actors
                from actor in actors.DefaultIfEmpty()
                orderby log.Id descending
                select new AuditLogDto(
                    log.Id,
                    log.OccurredAtUtc,
                    log.ActorUserId,
                    actor != null ? actor.Email : null,
                    log.Action,
                    log.EntityType,
                    log.EntityId,
                    log.Succeeded,
                    log.IpAddress,
                    log.Details))
            .ToListAsync(cancellationToken);

        return new PagedResult<AuditLogDto>(items, page, pageSize, totalCount);
    }
}
