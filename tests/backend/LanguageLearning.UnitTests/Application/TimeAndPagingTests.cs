using LanguageLearning.Application.Common.Paging;
using LanguageLearning.Application.Common.Time;

namespace LanguageLearning.UnitTests.Application;

public sealed class TimeAndPagingTests
{
    [Fact]
    public void TodayIn_uses_user_time_zone()
    {
        // 2026-11-05 17:30 UTC = 2026-11-06 01:30 台北時間
        var utcNow = new DateTimeOffset(2026, 11, 5, 17, 30, 0, TimeSpan.Zero);

        Assert.Equal(new DateOnly(2026, 11, 6), TimeZoneHelper.TodayIn("Asia/Taipei", utcNow));
        Assert.Equal(new DateOnly(2026, 11, 5), TimeZoneHelper.TodayIn("UTC", utcNow));
    }

    [Fact]
    public void TodayIn_falls_back_to_default_for_invalid_zone()
    {
        var utcNow = new DateTimeOffset(2026, 11, 5, 17, 30, 0, TimeSpan.Zero);

        Assert.Equal(
            TimeZoneHelper.TodayIn(TimeZoneHelper.DefaultTimeZoneId, utcNow),
            TimeZoneHelper.TodayIn("Invalid/Zone", utcNow));
    }

    [Theory]
    [InlineData(null, null, 1, Paging.DefaultPageSize)]
    [InlineData(0, 0, 1, Paging.DefaultPageSize)]
    [InlineData(3, 50, 3, 50)]
    [InlineData(2, 1000, 2, Paging.MaxPageSize)]
    public void Normalize_clamps_page_and_size(int? page, int? size, int expectedPage, int expectedSize)
    {
        var (normalizedPage, normalizedSize) = Paging.Normalize(page, size);

        Assert.Equal(expectedPage, normalizedPage);
        Assert.Equal(expectedSize, normalizedSize);
    }

    [Fact]
    public void PagedResult_computes_total_pages()
    {
        Assert.Equal(3, new PagedResult<int>([1], 1, 20, 41).TotalPages);
        Assert.Equal(0, new PagedResult<int>([], 1, 20, 0).TotalPages);
    }
}
