using LanguageLearning.Application.Common.Auditing;

namespace LanguageLearning.Application.Abstractions;

/// <summary>寫入稽核紀錄。實作須自行過濾敏感資料，且寫入失敗不得中斷主要流程。</summary>
public interface IAuditLogger
{
    Task WriteAsync(AuditEntry entry, CancellationToken cancellationToken);
}
