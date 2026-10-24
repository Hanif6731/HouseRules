using System.Text.RegularExpressions;

using HouseRules.Analyzers;
using HouseRules.Generate;

using Microsoft.CodeAnalysis.Diagnostics;

namespace HouseRules.Tests;

public sealed class CatalogueTests
{
    private const string FalsePositiveForm =
        "https://github.com/Hanif6731/HouseRules/issues/new?template=false-positive.yml";

    private static readonly string[] Levels = ["Baseline", "Recommended", "Strict"];

    [Fact]
    public void Generated_files_match_the_catalogue()
    {
        foreach (var (path, expected) in CatalogueGenerator.Generate(RepoRoot.Path))
        {
            var file = Path.Combine(RepoRoot.Path, path);
            var actual = File.Exists(file) ? File.ReadAllText(file).ReplaceLineEndings("\n") : "<missing>";
            Assert.True(
                expected == actual,
                $"{path} is out of date with catalogue/rules.json: run `dotnet run --project tools/Generate`.");
        }
    }

    [Fact]
    public void Every_level_is_a_superset_of_the_one_below()
    {
        for (var i = 1; i < Levels.Length; i++)
        {
            var lower = GlobalConfig(Levels[i - 1]);
            var higher = GlobalConfig(Levels[i]);
            foreach (var (key, value) in lower)
            {
                Assert.True(
                    higher.TryGetValue(key, out var higherValue) && higherValue == value,
                    $"{Levels[i]} drops or changes `{key} = {value}` from {Levels[i - 1]}.");
            }

            Assert.True(higher.Count > lower.Count, $"{Levels[i]} adds nothing to {Levels[i - 1]}.");
        }
    }

    [Fact]
    public void Every_entry_has_a_since_version()
    {
        var catalogue = CatalogueGenerator.Load(RepoRoot.Path);
        var versions = catalogue.Properties.Select(entry => (entry.Name, entry.Since))
            .Concat(catalogue.Categories.Select(entry => (entry.Name, entry.Since)))
            .Concat(catalogue.Banned.Select(entry => (entry.Category, entry.Since)))
            .Concat(catalogue.Rules.Select(entry => (entry.Id, entry.Since)));

        foreach (var (name, since) in versions)
        {
            Assert.True(
                since is not null && Regex.IsMatch(since, @"^\d+\.\d+\.\d+$"),
                $"{name} has no `since` version.");
        }
    }

    [Fact]
    public void Every_own_rule_has_a_docs_page()
    {
        foreach (var rule in CatalogueGenerator.Load(RepoRoot.Path).Rules.Where(rule => rule.Source == "HouseRules"))
        {
            var page = Path.Combine(RepoRoot.Path, "docs", "rules", rule.Id + ".md");
            Assert.True(File.Exists(page), $"{rule.Id} has no docs/rules/{rule.Id}.md.");
            Assert.True(
                File.ReadAllText(page).Contains(FalsePositiveForm, StringComparison.Ordinal),
                $"docs/rules/{rule.Id}.md does not link the false-positive issue form.");
        }
    }

    [Fact]
    public void Every_own_analyzer_rule_is_in_the_catalogue()
    {
        var shipped = typeof(RuleIds).Assembly.GetTypes()
            .Where(type => !type.IsAbstract && typeof(DiagnosticAnalyzer).IsAssignableFrom(type))
            .Select(type => (DiagnosticAnalyzer)Activator.CreateInstance(type)!)
            .SelectMany(analyzer => analyzer.SupportedDiagnostics)
            .Select(descriptor => descriptor.Id)
            .Order();

        var catalogued = CatalogueGenerator.Load(RepoRoot.Path)
            .Rules
            .Where(rule => rule.Source == "HouseRules")
            .Select(rule => rule.Id)
            .Order();

        Assert.Equal(catalogued, shipped);
    }

    [Fact]
    public void Every_ban_category_has_its_symbols_file()
    {
        foreach (var ban in CatalogueGenerator.Load(RepoRoot.Path).Banned)
        {
            var name = $"BannedSymbols.{ban.Category}.txt";
            var file = Path.Combine(RepoRoot.Path, CatalogueGenerator.ConfigFolder, name);
            Assert.True(File.Exists(file), $"Ban category {ban.Category} has no {file}.");
        }
    }

    private static Dictionary<string, string> GlobalConfig(string level) =>
        File.ReadAllLines(Path.Combine(RepoRoot.Path, CatalogueGenerator.ConfigFolder, level + ".globalconfig"))
            .Where(line => !line.StartsWith('#'))
            .Select(line => line.Split(" = ", 2))
            .Where(parts => parts.Length == 2 && parts[0] != "is_global" && parts[0] != "global_level")
            .ToDictionary(parts => parts[0], parts => parts[1]);
}
