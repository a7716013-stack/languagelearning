namespace LanguageLearning.Infrastructure.Options;

/// <summary>初始管理者帳號。只從 user-secrets 或環境變數提供，不寫入 appsettings。</summary>
public sealed class SeedOptions
{
    public const string SectionName = "Seed:Admin";

    public string? Email { get; set; }

    public string? Password { get; set; }

    public string DisplayName { get; set; } = "系統管理員";
}
