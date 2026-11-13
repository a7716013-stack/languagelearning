namespace LanguageLearning.Application.Admin;

public sealed record AdminSummaryDto(
    int TotalUsers,
    int ConfirmedUsers,
    int AdminCount,
    int NewUsersLast7Days,
    int SuccessfulLoginsLast24Hours,
    int FailedLoginsLast24Hours);

public sealed record AdminUserDto(
    Guid Id,
    string Email,
    string DisplayName,
    IReadOnlyList<string> Roles,
    bool EmailConfirmed,
    bool IsLockedOut,
    DateTime CreatedAtUtc,
    DateTime? LastLoginAtUtc);

public sealed record AuditLogDto(
    long Id,
    DateTime OccurredAtUtc,
    Guid? ActorUserId,
    string? ActorEmail,
    string Action,
    string? EntityType,
    string? EntityId,
    bool Succeeded,
    string? IpAddress,
    string? Details);

public sealed record AdminUserQuery(int? Page, int? PageSize, string? Search);

public sealed record AuditLogQuery(int? Page, int? PageSize, string? Action, DateTime? FromUtc, DateTime? ToUtc);
