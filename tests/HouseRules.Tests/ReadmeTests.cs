using System.Text.RegularExpressions;

namespace HouseRules.Tests;

public sealed partial class ReadmeTests
{
    [Fact]
    public void Every_readme_command_is_run_by_the_smoke_test_or_ci()
    {
        var readme = File.ReadAllText(Path.Combine(RepoRoot.Path, "README.md")).ReplaceLineEndings("\n");
        string[] exercised =
        [
            .. Directory.GetFiles(Path.Combine(RepoRoot.Path, ".github", "workflows"), "*.yml")
                .Append(Path.Combine(RepoRoot.Path, "tests", "smoke", "smoke.sh"))
                .Select(File.ReadAllText),
        ];

        var blocks = CodeBlock().Matches(readme).ToList();

        Assert.NotEmpty(blocks);
        Assert.All(
            blocks,
            block => Assert.True(
                IsExercised(block.Groups["language"].Value, block.Groups["body"].Value, exercised),
                $"README block `{block.Groups["body"].Value.Trim()}` is not run by tests/smoke/smoke.sh or a workflow."));
    }

    [Theory]
    [InlineData("bash", "dotnet build", "output=$(cd \"$1\" && dotnet build 2>&1) || code=$?", true)]
    [InlineData("bash", "dotnet build", "(cd \"$dir\" && dotnet build -p:HouseRulesBaseline=init)", false)]
    [InlineData("bash", "dotnet test", "      - run: dotnet test -c Release --no-build", true)]
    [InlineData("bash", "dotnet run --project a", "  run: dotnet run --project a -c Release && git diff", true)]
    [InlineData("bash", "dotnet format", "(cd \"$dir\" && dotnet format --verify-no-changes)", false)]
    [InlineData("xml", "<Project>\n  <Import />\n</Project>", "<Project>\n  <Import />\n</Project>", true)]
    [InlineData("xml", "<Project>\n  <Import />\n</Project>", "<Project>\n  <A />\n  <Import />\n</Project>", false)]
    public void A_block_counts_only_when_its_whole_commands_or_snippet_are_run(
        string language,
        string block,
        string exercised,
        bool expected)
    {
        Assert.Equal(expected, IsExercised(language, block, [exercised]));
    }

    // A bash line counts when a script or workflow runs exactly that command; an XML snippet counts when a script
    // contains all of it, line after line.
    private static bool IsExercised(string language, string block, IReadOnlyList<string> exercised)
    {
        if (language == "bash")
        {
            var commands = exercised.SelectMany(Lines).SelectMany(CommandsOf).ToHashSet(StringComparer.Ordinal);
            return Lines(block).All(line => commands.Contains(Canonical(line)));
        }

        var snippet = string.Join("\n", Lines(block));
        return exercised.Any(text => string.Join("\n", Lines(text)).Contains(snippet, StringComparison.Ordinal));
    }

    private static IEnumerable<string> Lines(string text) =>
        text.ReplaceLineEndings("\n").Split('\n').Select(line => line.Trim()).Where(line => line.Length > 0);

    private static IEnumerable<string> CommandsOf(string line) =>
        CommandSeparator().Split(WorkflowRunKey().Replace(line, "")).Select(Canonical);

    private static string Canonical(string command) =>
        BuildReuseOption().Replace(Redirection().Replace(command, ""), "").Trim();

    [GeneratedRegex("^```(?<language>bash|xml)\\n(?<body>.*?)^```", RegexOptions.Multiline | RegexOptions.Singleline)]
    private static partial Regex CodeBlock();

    [GeneratedRegex("^(?:- )?run: ")]
    private static partial Regex WorkflowRunKey();

    [GeneratedRegex("\\$\\(|&&|\\|\\||[|;()]")]
    private static partial Regex CommandSeparator();

    [GeneratedRegex("\\s+\\d*>(?:&\\d+|\\s*\\S+)")]
    private static partial Regex Redirection();

    // CI builds once in Release and reuses that build; these options do not change what a command checks.
    [GeneratedRegex("\\s+(?:-c Release|--no-build|--no-restore)(?=\\s|$)")]
    private static partial Regex BuildReuseOption();
}
