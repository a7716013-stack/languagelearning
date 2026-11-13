namespace LanguageLearning.Application.Abstractions;

public interface IUserTimeZoneResolver
{
    /// <summary>取得使用者設定的 IANA 時區代碼；找不到時回傳預設時區。</summary>
    Task<string> GetTimeZoneIdAsync(Guid userId, CancellationToken cancellationToken);
}
