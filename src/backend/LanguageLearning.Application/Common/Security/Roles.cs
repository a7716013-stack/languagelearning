namespace LanguageLearning.Application.Common.Security;

public static class Roles
{
    public const string Admin = "Admin";
    public const string Student = "Student";

    public static readonly IReadOnlyList<string> All = [Admin, Student];
}

public static class Policies
{
    public const string AdminOnly = "AdminOnly";
}
