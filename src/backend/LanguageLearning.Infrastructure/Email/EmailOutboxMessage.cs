using LanguageLearning.Application.Abstractions;

namespace LanguageLearning.Infrastructure.Email;

public enum EmailOutboxStatus
{
    Pending = 1,
    Processing = 2,
    Sent = 3,
    Failed = 4,
    Skipped = 5,
}

/// <summary>
/// 寄件匣中的一封信。刻意不保存收件者、內容、連結或 Token：寄出時才依使用者資料與種類組成信件。
/// </summary>
public sealed class EmailOutboxMessage
{
    public Guid Id { get; set; }

    public AccountEmailKind Kind { get; set; }

    public Guid UserId { get; set; }

    public EmailOutboxStatus Status { get; set; } = EmailOutboxStatus.Pending;

    /// <summary>已嘗試寄送的次數。</summary>
    public int Attempts { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime NextAttemptAtUtc { get; set; }

    public DateTime? LastAttemptAtUtc { get; set; }

    public DateTime? CompletedAtUtc { get; set; }

    /// <summary>最後一次失敗的原因（已遮蔽 Email，長度有限制）。</summary>
    public string? LastError { get; set; }

    public byte[] RowVersion { get; set; } = [];
}
