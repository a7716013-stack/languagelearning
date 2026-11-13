using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace LanguageLearning.Infrastructure.Persistence.Converters;

/// <summary>
/// 寫入時一律轉為 UTC，讀出時標記為 <see cref="DateTimeKind.Utc"/>，避免時區誤判。
/// </summary>
public sealed class UtcDateTimeConverter : ValueConverter<DateTime, DateTime>
{
    public UtcDateTimeConverter()
        : base(
            value => value.Kind == DateTimeKind.Local ? value.ToUniversalTime() : DateTime.SpecifyKind(value, DateTimeKind.Utc),
            value => DateTime.SpecifyKind(value, DateTimeKind.Utc))
    {
    }
}
