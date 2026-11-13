namespace LanguageLearning.Domain.Common;

/// <summary>
/// 違反領域規則時拋出。<see cref="Code"/> 為穩定的錯誤代碼，<see cref="Field"/> 為對應的輸入欄位（可為 null）。
/// </summary>
public sealed class DomainException : Exception
{
    public DomainException(string code, string message, string? field = null)
        : base(message)
    {
        Code = code;
        Field = field;
    }

    public string Code { get; }

    public string? Field { get; }
}
