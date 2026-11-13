namespace LanguageLearning.Domain.Auditing;

public static class AuditActions
{
    public const string Register = "auth.register";
    public const string RegisterDuplicate = "auth.register.duplicate";
    public const string EmailConfirmed = "auth.email.confirmed";
    public const string EmailConfirmFailed = "auth.email.confirm_failed";
    public const string LoginSucceeded = "auth.login.succeeded";
    public const string LoginFailed = "auth.login.failed";
    public const string LoginLockedOut = "auth.login.locked_out";
    public const string LoginNotConfirmed = "auth.login.email_not_confirmed";
    public const string Logout = "auth.logout";
    public const string PasswordResetRequested = "auth.password.reset_requested";
    public const string PasswordReset = "auth.password.reset";
    public const string PasswordResetFailed = "auth.password.reset_failed";
    public const string PasswordChanged = "auth.password.changed";
    public const string ProfileUpdated = "user.profile.updated";
    public const string ExamGoalSaved = "user.exam_goal.saved";
    public const string OnboardingCompleted = "user.onboarding.completed";
    public const string AdminSeeded = "admin.seeded";
}
