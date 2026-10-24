using System.Text.RegularExpressions;

namespace HouseRules.Tests;

public sealed partial class ReadmeTests
{
    [Fact]
    public void Every_readme_command_is_run_by_the_smoke_test_or_ci()
    {
        var readme = File.ReadAllText(Path.Combine(RepoRoot.Path, "README.md")).ReplaceLineEndings("\n");
        var exercised = string.Join(
            "\n",
            Directory.GetFiles(Path.Combine(RepoRoot.Path, ".github", "workflows"), "*.yml")
                .Append(Path.Combine(RepoRoot.Path, "tests", "smoke", "smoke.sh"))
                .Select(File.ReadAllText));

        var lines = CodeBlock().Matches(readme)
            .SelectMany(block => block.Groups["body"].Value.Split('\n'))
            .Select(line => line.Trim())
            .Where(line => line.Length > 0)
            .ToList();

        Assert.NotEmpty(lines);
        Assert.All(
            lines,
            line => Assert.True(
                exercised.Contains(line, StringComparison.Ordinal),
                $"README line `{line}` is not run by tests/smoke/smoke.sh or a workflow."));
    }

    [GeneratedRegex("^```(?:bash|xml)\\n(?<body>.*?)^```", RegexOptions.Multiline | RegexOptions.Singleline)]
    private static partial Regex CodeBlock();
}
