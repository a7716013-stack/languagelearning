using LanguageLearning.Infrastructure.Identity;
using LanguageLearning.Infrastructure.Persistence.Converters;

namespace LanguageLearning.UnitTests.Infrastructure;

public sealed class InfrastructureTests
{
    [Fact]
    public void UtcDateTimeConverter_marks_values_as_utc()
    {
        var converter = new UtcDateTimeConverter();
        var unspecified = new DateTime(2026, 11, 5, 8, 0, 0, DateTimeKind.Unspecified);

        var fromStore = (DateTime)converter.ConvertFromProvider(unspecified)!;
        var toStore = (DateTime)converter.ConvertToProvider(unspecified)!;

        Assert.Equal(DateTimeKind.Utc, fromStore.Kind);
        Assert.Equal(unspecified.Ticks, fromStore.Ticks);
        Assert.Equal(DateTimeKind.Utc, toStore.Kind);
    }

    [Fact]
    public void UtcDateTimeConverter_converts_local_time_to_utc()
    {
        var converter = new UtcDateTimeConverter();
        var local = new DateTime(2026, 11, 5, 8, 0, 0, DateTimeKind.Local);

        var toStore = (DateTime)converter.ConvertToProvider(local)!;

        Assert.Equal(local.ToUniversalTime(), toStore);
    }

    [Fact]
    public void ErrorDescriber_keeps_codes_and_translates_descriptions()
    {
        var describer = new ZhTwIdentityErrorDescriber();

        var mismatch = describer.PasswordMismatch();
        var tooShort = describer.PasswordTooShort(10);

        Assert.Equal("PasswordMismatch", mismatch.Code);
        Assert.Equal("密碼不正確。", mismatch.Description);
        Assert.Equal("PasswordTooShort", tooShort.Code);
        Assert.Contains("10", tooShort.Description, StringComparison.Ordinal);
    }
}
