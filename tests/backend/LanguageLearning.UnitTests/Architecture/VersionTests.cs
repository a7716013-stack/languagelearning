using System.Reflection;
using System.Text.Json;
using System.Xml.Linq;
using LanguageLearning.Domain.Exams;

namespace LanguageLearning.UnitTests.Architecture;

/// <summary>前後端版本號必須一致（後端：Directory.Build.props；前端：src/frontend/package.json）。</summary>
public sealed class VersionTests
{
    [Fact]
    public void Backend_and_frontend_versions_match()
    {
        var root = FindRepositoryRoot();
        var backendVersion = XDocument.Load(Path.Combine(root, "Directory.Build.props"))
            .Descendants("Version")
            .Single()
            .Value;

        using var packageJson = JsonDocument.Parse(File.ReadAllText(Path.Combine(root, "src", "frontend", "package.json")));
        var frontendVersion = packageJson.RootElement.GetProperty("version").GetString();

        Assert.Equal(backendVersion, frontendVersion);
    }

    [Fact]
    public void Assemblies_use_product_version()
    {
        var root = FindRepositoryRoot();
        var backendVersion = XDocument.Load(Path.Combine(root, "Directory.Build.props"))
            .Descendants("Version")
            .Single()
            .Value;

        var informational = typeof(ExamType).Assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
            .InformationalVersion;

        Assert.NotNull(informational);
        Assert.Equal(backendVersion, informational.Split('+')[0]);
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "LanguageLearning.slnx")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new InvalidOperationException("找不到 Repository 根目錄（LanguageLearning.slnx）。");
    }
}
