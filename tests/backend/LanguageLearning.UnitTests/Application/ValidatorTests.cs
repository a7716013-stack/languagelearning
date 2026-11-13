using LanguageLearning.Application.Auth;
using LanguageLearning.Application.Goals;
using LanguageLearning.Application.Users;

namespace LanguageLearning.UnitTests.Application;

public sealed class ValidatorTests
{
    private const string StrongPassword = "Correct1Horse";

    [Fact]
    public void Register_accepts_valid_request()
    {
        var result = new RegisterRequestValidator().Validate(
            new RegisterRequest("student@example.com", StrongPassword, "小明"));

        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-an-email")]
    public void Register_rejects_invalid_email(string email)
    {
        var result = new RegisterRequestValidator().Validate(new RegisterRequest(email, StrongPassword, "小明"));

        Assert.Contains(result.Errors, e => e.PropertyName == nameof(RegisterRequest.Email));
    }

    [Theory]
    [InlineData("Short1A")]
    [InlineData("alllowercase1")]
    [InlineData("ALLUPPERCASE1")]
    [InlineData("NoDigitsHere")]
    public void Register_rejects_weak_passwords(string password)
    {
        var result = new RegisterRequestValidator().Validate(
            new RegisterRequest("student@example.com", password, "小明"));

        Assert.Contains(result.Errors, e => e.PropertyName == nameof(RegisterRequest.Password));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Register_rejects_blank_display_name(string displayName)
    {
        var result = new RegisterRequestValidator().Validate(
            new RegisterRequest("student@example.com", StrongPassword, displayName));

        Assert.Contains(result.Errors, e => e.PropertyName == nameof(RegisterRequest.DisplayName));
    }

    [Fact]
    public void Register_error_messages_are_traditional_chinese()
    {
        var result = new RegisterRequestValidator().Validate(new RegisterRequest("bad", "x", ""));

        Assert.All(result.Errors, error => Assert.Matches(@"[一-鿿]", error.ErrorMessage));
    }

    [Fact]
    public void ChangePassword_rejects_same_password()
    {
        var result = new ChangePasswordRequestValidator().Validate(
            new ChangePasswordRequest(StrongPassword, StrongPassword));

        Assert.Contains(result.Errors, e => e.PropertyName == nameof(ChangePasswordRequest.NewPassword));
    }

    [Fact]
    public void UpdateProfile_accepts_iana_time_zone_and_known_theme()
    {
        var result = new UpdateProfileRequestValidator().Validate(
            new UpdateProfileRequest("小明", "Asia/Taipei", UiThemes.Dark));

        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData("Mars/Olympus_Mons", "dark", nameof(UpdateProfileRequest.TimeZoneId))]
    [InlineData("Asia/Taipei", "neon", nameof(UpdateProfileRequest.UiTheme))]
    public void UpdateProfile_rejects_invalid_values(string timeZoneId, string theme, string property)
    {
        var result = new UpdateProfileRequestValidator().Validate(
            new UpdateProfileRequest("小明", timeZoneId, theme));

        Assert.Contains(result.Errors, e => e.PropertyName == property);
    }

    [Theory]
    [InlineData(9)]
    [InlineData(601)]
    public void UpsertGoal_rejects_daily_minutes_out_of_range(int minutes)
    {
        var result = new UpsertExamGoalRequestValidator().Validate(
            new UpsertExamGoalRequest(800, new DateOnly(2027, 1, 1), minutes, null));

        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpsertExamGoalRequest.DailyStudyMinutes));
    }

    [Fact]
    public void UpsertGoal_requires_exam_date()
    {
        var result = new UpsertExamGoalRequestValidator().Validate(
            new UpsertExamGoalRequest(800, default, 60, null));

        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpsertExamGoalRequest.ExamDate));
    }
}
