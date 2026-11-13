namespace LanguageLearning.Application.Common.Time;

public static class TimeZoneHelper
{
    public const string DefaultTimeZoneId = "Asia/Taipei";

    public static bool IsValid(string? timeZoneId) =>
        !string.IsNullOrWhiteSpace(timeZoneId) && TimeZoneInfo.TryFindSystemTimeZoneById(timeZoneId, out _);

    public static TimeZoneInfo FindOrDefault(string? timeZoneId)
    {
        if (!string.IsNullOrWhiteSpace(timeZoneId) && TimeZoneInfo.TryFindSystemTimeZoneById(timeZoneId, out var zone))
        {
            return zone;
        }

        return TimeZoneInfo.TryFindSystemTimeZoneById(DefaultTimeZoneId, out var fallback) ? fallback : TimeZoneInfo.Utc;
    }

    /// <summary>以指定時區計算「今天」的日期。</summary>
    public static DateOnly TodayIn(string? timeZoneId, DateTimeOffset utcNow)
    {
        var local = TimeZoneInfo.ConvertTime(utcNow, FindOrDefault(timeZoneId));
        return DateOnly.FromDateTime(local.DateTime);
    }
}
