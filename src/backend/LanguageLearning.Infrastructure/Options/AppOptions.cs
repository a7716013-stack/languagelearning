using System.ComponentModel.DataAnnotations;

namespace LanguageLearning.Infrastructure.Options;

public sealed class AppOptions
{
    public const string SectionName = "App";

    /// <summary>前端對外網址，用於組成信件中的連結（例如 http://localhost:5173）。</summary>
    [Required]
    [Url]
    public string PublicBaseUrl { get; set; } = string.Empty;

    /// <summary>是否將 HTTP 轉向 HTTPS。本機開發（Vite proxy）與測試時關閉。</summary>
    public bool UseHttpsRedirection { get; set; } = true;
}
