namespace LanguageLearning.Application.Common.Auditing;

public sealed record AuditEntry(
    string Action,
    bool Succeeded,
    Guid? ActorUserId = null,
    string? EntityType = null,
    string? EntityId = null,
    IReadOnlyDictionary<string, object?>? Details = null);
