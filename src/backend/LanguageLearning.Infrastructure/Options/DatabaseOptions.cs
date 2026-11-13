namespace LanguageLearning.Infrastructure.Options;

public sealed class DatabaseOptions
{
    public const string SectionName = "Database";

    /// <summary>啟動時自動套用 Migration。僅建議在 Development / Testing 使用。</summary>
    public bool ApplyMigrationsOnStartup { get; set; }
}
