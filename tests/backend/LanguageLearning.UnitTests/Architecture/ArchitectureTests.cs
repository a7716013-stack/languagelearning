using System.Reflection;
using LanguageLearning.Application;
using LanguageLearning.Domain.Exams;
using LanguageLearning.Infrastructure.Persistence;

namespace LanguageLearning.UnitTests.Architecture;

/// <summary>確保分層相依方向：Domain ← Application ← Infrastructure ← Api。</summary>
public sealed class ArchitectureTests
{
    private static readonly Assembly Domain = typeof(ExamType).Assembly;
    private static readonly Assembly Application = typeof(DependencyInjection).Assembly;
    private static readonly Assembly Infrastructure = typeof(AppDbContext).Assembly;

    [Fact]
    public void Domain_does_not_depend_on_other_layers_or_frameworks()
    {
        var references = ReferenceNames(Domain);

        Assert.DoesNotContain(references, name => name.StartsWith("LanguageLearning.", StringComparison.Ordinal));
        Assert.DoesNotContain(references, name => name.StartsWith("Microsoft.EntityFrameworkCore", StringComparison.Ordinal));
        Assert.DoesNotContain(references, name => name.StartsWith("Microsoft.AspNetCore", StringComparison.Ordinal));
    }

    [Fact]
    public void Application_depends_only_on_domain()
    {
        var references = ReferenceNames(Application);

        Assert.Contains("LanguageLearning.Domain", references);
        Assert.DoesNotContain("LanguageLearning.Infrastructure", references);
        Assert.DoesNotContain("LanguageLearning.Api", references);
        Assert.DoesNotContain(references, name => name.StartsWith("Microsoft.AspNetCore", StringComparison.Ordinal));
    }

    [Fact]
    public void Infrastructure_does_not_depend_on_api()
    {
        Assert.DoesNotContain("LanguageLearning.Api", ReferenceNames(Infrastructure));
    }

    private static List<string> ReferenceNames(Assembly assembly) =>
        assembly.GetReferencedAssemblies().Select(reference => reference.Name ?? string.Empty).ToList();
}
