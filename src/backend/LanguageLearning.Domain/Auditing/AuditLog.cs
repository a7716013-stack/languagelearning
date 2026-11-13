namespace LanguageLearning.Domain.Auditing;

/// <summary>
/// 稽核紀錄。只保存必要欄位，不得包含密碼、Token、Cookie 等敏感資料。
/// </summary>
public sealed class AuditLog
{
    public long Id { get; set; }

    public DateTime OccurredAtUtc { get; set; }

    public Guid? ActorUserId { get; set; }

    public string Action { get; set; } = string.Empty;

    public string? EntityType { get; set; }

    public string? EntityId { get; set; }

    public bool Succeeded { get; set; }

    public string? IpAddress { get; set; }

    /// <summary>已過濾敏感資料的 JSON 字串。</summary>
    public string? Details { get; set; }
}
