using HouseRules.Analyzers.Rules;
using HouseRules.CodeFixes;
using HouseRules.Tasks;

namespace HouseRules.Tests;

/// <summary>
/// Each assembly runs in a different host: the compiler loads the analyzers into every consumer build, the IDE and
/// <c>dotnet format</c> load the code fixes, MSBuild loads the task. A reference that host does not provide breaks the
/// consumer's build, so each assembly may use only its host's API.
/// </summary>
public sealed class ArchitectureTests
{
    [Fact]
    public void The_analyzers_reference_only_the_compiler_api()
    {
        string[] allowed = ["netstandard", "Microsoft.CodeAnalysis", "Microsoft.CodeAnalysis.CSharp"];

        Assert.All(
            References(typeof(RuleIds)),
            name => Assert.True(
                allowed.Contains(name) || name.StartsWith("System.", StringComparison.Ordinal),
                $"The analyzers reference {name}; the compiler does not provide it."));
    }

    [Fact]
    public void The_code_fixes_use_workspaces_but_not_msbuild()
    {
        var references = References(typeof(OneElementPerLineOnceWrappedCodeFix));

        Assert.Contains("Microsoft.CodeAnalysis.Workspaces", references);
        Assert.DoesNotContain(references, name => name.StartsWith("Microsoft.Build", StringComparison.Ordinal));
    }

    [Fact]
    public void The_msbuild_task_does_not_load_the_compiler()
    {
        Assert.DoesNotContain(
            References(typeof(Baseline)),
            name => name.StartsWith("Microsoft.CodeAnalysis", StringComparison.Ordinal));
    }

    private static IReadOnlyList<string> References(Type type) =>
        [.. type.Assembly.GetReferencedAssemblies().Select(reference => reference.Name ?? "")];
}
