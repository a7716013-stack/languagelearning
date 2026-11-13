using System.Reflection;

namespace LanguageLearning.Api.Common;

/// <summary>產品版本，來源為 Directory.Build.props 的 &lt;Version&gt;。</summary>
public static class AppVersion
{
    private static readonly string Informational =
        typeof(AppVersion).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
        ?? "0.0.0";

    public static string Version => Informational.Split('+')[0];

    public static string? Commit
    {
        get
        {
            var plus = Informational.IndexOf('+', StringComparison.Ordinal);
            if (plus < 0 || plus == Informational.Length - 1)
            {
                return null;
            }

            var commit = Informational[(plus + 1)..];
            return commit.Length > 12 ? commit[..12] : commit;
        }
    }
}

public sealed record VersionResponse(string Version, string Environment, string? Commit);

public sealed record MessageResponse(string Message);
