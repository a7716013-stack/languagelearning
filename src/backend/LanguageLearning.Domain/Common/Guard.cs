namespace LanguageLearning.Domain.Common;

/// <summary>Domain 內部共用的參數檢查（程式錯誤用 Argument 例外；使用者可修正的錯誤用 <see cref="DomainException"/>）。</summary>
internal static class Guard
{
    public static void Utc(DateTime value, string parameterName)
    {
        if (value.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException("時間必須為 UTC。", parameterName);
        }
    }

    public static void NotEmpty(Guid value, string parameterName)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException("Id 不可為空。", parameterName);
        }
    }

    /// <summary>去除前後空白；空字串視為 null。</summary>
    public static string? Optional(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
