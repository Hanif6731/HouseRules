using HouseRules.Analyzers;
using HouseRules.Tasks;

namespace HouseRules.Tests;

public sealed class BaselineTests
{
    private static readonly string Root = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "baseline-tests"));

    private static readonly string FileA = Path.Combine(Root, "A.cs");

    private static readonly string[] LinesOfA =
        ["class C", "{", "    void M()", "    {", "        Call(2);", "    }", "}"];

    [Fact]
    public void Init_records_every_finding_that_is_not_already_suppressed()
    {
        Finding[] findings =
        [
            new(FileA, 5, "HR0001", Suppressed: false),
            new(FileA, 5, "HR0001", Suppressed: false),
            new(FileA, 3, "CA1822", Suppressed: true),
        ];

        var recorded = Baseline.Init(findings, Root, ReadLines);

        Assert.Equal(new Dictionary<string, int> { [BaselineKey.Of("A.cs", "HR0001", "Call(2);")] = 2 }, recorded);
    }

    [Fact]
    public void Findings_the_suppressor_cannot_suppress_are_named_instead_of_recorded()
    {
        Finding[] findings =
        [
            new(FileA, 5, "HR0001", Suppressed: false),
            new(FileA, 5, "LEGACY001", Suppressed: false),
            new("", 0, "CS1234", Suppressed: false),
            new(FileA, 3, "OTHER001", Suppressed: true),
        ];

        var unrecordable = Baseline.Unrecordable(findings, id => id is "HR0001" or "CS1234");

        Assert.Equal(["CS1234", "LEGACY001"], unrecordable);
    }

    [Fact]
    public void Prune_drops_fixed_entries_and_never_adds_new_ones()
    {
        var recorded = new Dictionary<string, int>
        {
            [BaselineKey.Of("A.cs", "HR0001", "Call(2);")] = 2,
            [BaselineKey.Of("A.cs", "CA1822", "void Fixed() { }")] = 1,
        };

        Finding[] findings = [new(FileA, 5, "HR0001", Suppressed: true), new(FileA, 3, "IDE0005", Suppressed: false)];

        var pruned = Baseline.Prune(recorded, findings, Root, ReadLines);

        Assert.Equal(new Dictionary<string, int> { [BaselineKey.Of("A.cs", "HR0001", "Call(2);")] = 1 }, pruned);
    }

    [Fact]
    public void A_baseline_round_trips_through_its_file_format()
    {
        var recorded = new Dictionary<string, int>
        {
            [BaselineKey.Of("src/B.cs", "CS0168", "int unused;")] = 1,
            [BaselineKey.Of("A.cs", "HR0001", "Call(2);")] = 2,
        };

        var text = Baseline.Format(recorded);

        Assert.Equal(recorded, Baseline.Parse(text));
        Assert.Equal(3, text.Split('\n', StringSplitOptions.RemoveEmptyEntries).Count(line => !line.StartsWith('#')));
    }

    [Fact]
    public void A_key_is_the_relative_path_the_rule_and_a_hash_of_the_trimmed_line()
    {
        var key = BaselineKey.Of(BaselineKey.RelativePath(Root, Path.Combine(Root, "src", "B.cs")), "CS0168", "  x;  ");

        Assert.Equal("src/B.cs|CS0168|" + BaselineKey.Of("", "", "x;").Split('|')[2], key);
        Assert.Matches("^src/B.cs\\|CS0168\\|[0-9a-f]{64}$", key);
    }

    [Fact]
    public void A_file_linked_from_outside_the_baseline_folder_is_keyed_on_a_relative_path()
    {
        var sibling = Path.Combine(Root, "..", "shared", "Linked.cs");
        var above = Path.Combine(Root, "Linked.cs");

        Assert.Equal("../shared/Linked.cs", BaselineKey.RelativePath(Root, sibling));
        Assert.Equal("../../Linked.cs", BaselineKey.RelativePath(Path.Combine(Root, "a", "b"), above));
    }

    [Fact]
    public void Findings_are_read_from_a_compiler_error_log()
    {
        var uri = new Uri(FileA).AbsoluteUri;
        var sarif = $$"""
            {
              "version": "2.1.0",
              "runs": [ { "results": [
                { "ruleId": "HR0001", "level": "warning",
                  "locations": [ { "physicalLocation": { "artifactLocation": { "uri": "{{uri}}" },
                    "region": { "startLine": 5 } } } ] },
                { "ruleId": "CA1822", "level": "warning", "suppressions": [ { "kind": "inSource" } ],
                  "locations": [ { "physicalLocation": { "artifactLocation": { "uri": "{{uri}}" },
                    "region": { "startLine": 3 } } } ] },
                { "ruleId": "CS8032", "level": "warning" },
                { "ruleId": "CS1030", "level": "warning",
                  "locations": [ { "physicalLocation": { "artifactLocation": { "uri": "Page.razor" },
                    "region": { "startLine": 2 } } } ] }
              ] } ]
            }
            """;

        var findings = Baseline.ReadErrorLog(sarif);

        Assert.Equal(
            [
                new(FileA, 5, "HR0001", Suppressed: false),
                new(FileA, 3, "CA1822", Suppressed: true),
                new("", 0, "CS8032", Suppressed: false),
                new("", 0, "CS1030", Suppressed: false),
            ],
            findings);
    }

    [Fact]
    public void Init_records_only_findings_that_fail_or_warn_the_build()
    {
        var findings = Baseline.ReadErrorLog(
            ErrorLog(("SYSLIB1045", "note", 5), ("IDE0005", "none", 3), ("HR0001", "warning", 5)));

        var recorded = Baseline.Init(findings, Root, ReadLines);
        var unrecordable = Baseline.Unrecordable(findings, id => id is "HR0001");

        Assert.Equal(new Dictionary<string, int> { [BaselineKey.Of("A.cs", "HR0001", "Call(2);")] = 1 }, recorded);
        Assert.Empty(unrecordable);
    }

    [Fact]
    public void An_error_the_suppressor_cannot_suppress_is_still_named()
    {
        var findings = Baseline.ReadErrorLog(ErrorLog(("SYSLIB1045", "note", 5), ("LEGACY001", "error", 5)));

        var unrecordable = Baseline.Unrecordable(findings, id => id is "HR0001");

        Assert.Equal(["LEGACY001"], unrecordable);
    }

    private static string ErrorLog(params (string RuleId, string Level, int Line)[] results)
    {
        var uri = new Uri(FileA).AbsoluteUri;
        var entries = results.Select(
            result => $$"""
                { "ruleId": "{{result.RuleId}}", "level": "{{result.Level}}",
                  "locations": [ { "physicalLocation": { "artifactLocation": { "uri": "{{uri}}" },
                    "region": { "startLine": {{result.Line}} } } } ] }
                """);

        return $$"""{ "version": "2.1.0", "runs": [ { "results": [ {{string.Join(",", entries)}} ] } ] }""";
    }

    private static string[] ReadLines(string path) => path == FileA ? LinesOfA : [];
}
