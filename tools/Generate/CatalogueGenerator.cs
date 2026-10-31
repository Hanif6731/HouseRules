using System.Text;
using System.Text.Json;

namespace HouseRules.Generate;

/// <summary>Turns <c>catalogue/rules.json</c> into every file that depends on it.</summary>
public static class CatalogueGenerator
{
    public const string CataloguePath = "catalogue/rules.json";

    public const string ConfigFolder = "src/HouseRules.Analyzers.Package/config";

    public const string PropsPath = "src/HouseRules.Analyzers.Package/buildTransitive/HouseRules.Analyzers.props";

    public const string LevelsDocPath = "docs/levels.md";

    public const string SuppressibleIdsPath = "src/HouseRules.Analyzers/Suppressors/SuppressibleIds.txt";

    public static readonly IReadOnlyList<string> Levels = ["Baseline", "Recommended", "Strict"];

    private const string GeneratedNote = "Generated from catalogue/rules.json by tools/Generate. Do not edit.";

    private const string CodeAnalysisDocs = "https://learn.microsoft.com/dotnet/fundamentals/code-analysis/";

    // Microsoft.NET.Test.Sdk sets IsTestProject; test projects on Microsoft.Testing.Platform alone set only the other.
    private const string TestProject = "'$(IsTestProject)' == 'true' or '$(IsTestingPlatformApplication)' == 'true'";

    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public static Catalogue Load(string repoRoot)
    {
        var json = File.ReadAllText(Path.Combine(repoRoot, CataloguePath));
        var catalogue = JsonSerializer.Deserialize<Catalogue>(json, JsonOptions)
            ?? throw new InvalidDataException(CataloguePath + " is empty.");

        Validate(catalogue);
        return catalogue;
    }

    /// <returns>Repo-relative path (with <c>/</c>) to file content (LF line endings).</returns>
    public static IReadOnlyDictionary<string, string> Generate(string repoRoot)
    {
        var catalogue = Load(repoRoot);
        var files = new SortedDictionary<string, string>(StringComparer.Ordinal);
        foreach (var level in Levels)
        {
            files[$"{ConfigFolder}/{level}.globalconfig"] = GlobalConfig(catalogue, level);
            files[$"{ConfigFolder}/{level}.ruleset"] = RuleSet(catalogue, level);
        }

        files[$"{ConfigFolder}/Tests.globalconfig"] = TestsGlobalConfig(catalogue);
        files[PropsPath] = Props(catalogue);
        files[LevelsDocPath] = LevelsDoc(catalogue);
        files[SuppressibleIdsPath] = string.Concat(SuppressibleIds.All(catalogue).Select(id => id + "\n"));
        return files;
    }

    public static int Rank(string level)
    {
        var index = Levels.ToList().IndexOf(level);
        return index < 0 ? throw new InvalidDataException($"Unknown level '{level}'.") : index + 1;
    }

    private static void Validate(Catalogue catalogue)
    {
        var levels = catalogue.Properties.Select(entry => entry.Level)
            .Concat(catalogue.Categories.Select(entry => entry.Level))
            .Concat(catalogue.Banned.Select(entry => entry.Level))
            .Concat(catalogue.Rules.Select(entry => entry.Level));

        foreach (var level in levels)
        {
            _ = Rank(level);
        }

        var duplicateKey = catalogue.Rules.Select(rule => $"dotnet_diagnostic.{rule.Id}.severity")
            .Concat(catalogue.Rules.SelectMany(rule => rule.Options?.Keys ?? []))
            .GroupBy(key => key)
            .FirstOrDefault(group => group.Count() > 1);

        if (duplicateKey is not null)
        {
            throw new InvalidDataException($"'{duplicateKey.Key}' is configured twice in {CataloguePath}.");
        }

        var fixable = catalogue.Rules.Where(rule => rule.Fixable)
            .Select(rule => $"dotnet_diagnostic.{rule.Id}.severity")
            .ToHashSet(StringComparer.Ordinal);

        var shadowed = catalogue.Tests.FirstOrDefault(entry => fixable.Contains(entry.Key));
        if (shadowed is not null)
        {
            throw new InvalidDataException(
                $"Test relaxation '{shadowed.Key}' targets a fixable rule; the level rule set would override it.");
        }
    }

    private static IEnumerable<RuleEntry> RulesUpTo(Catalogue catalogue, string level) =>
        catalogue.Rules.Where(rule => Rank(rule.Level) <= Rank(level));

    private static string GlobalConfig(Catalogue catalogue, string level)
    {
        var text = new Text();
        text.Line("# " + GeneratedNote);
        text.Line("is_global = true");

        // Above the SDK's own analysis-level configs (-99/-100), below any global config of the consuming repo (0+).
        text.Line("global_level = -50");
        foreach (var rule in RulesUpTo(catalogue, level))
        {
            text.Line();
            text.Line($"# {rule.Id}: {rule.Description}");
            text.Line($"dotnet_diagnostic.{rule.Id}.severity = {rule.Severity}");
            foreach (var (key, value) in rule.Options ?? new Dictionary<string, string>())
            {
                text.Line($"{key} = {value}");
            }
        }

        return text.ToString();
    }

    private static string TestsGlobalConfig(Catalogue catalogue)
    {
        var text = new Text();
        text.Line("# " + GeneratedNote);
        text.Line("is_global = true");

        // Above the level configs, so these relaxations win in test projects.
        text.Line("global_level = -40");
        foreach (var entry in catalogue.Tests)
        {
            text.Line();
            text.Line("# " + entry.Description);
            text.Line($"{entry.Key} = {entry.Value}");
        }

        return text.ToString();
    }

    // `dotnet format` ignores severities from global configs; a rule set carries them to it. It lists the fixable
    // rules only: a rule set outranks global configs, so a rule listed here could not be relaxed by Tests.globalconfig.
    private static string RuleSet(Catalogue catalogue, string level)
    {
        var text = new Text();
        text.Line("""<?xml version="1.0" encoding="utf-8"?>""");
        text.Line($"<!-- {GeneratedNote} -->");
        text.Line($"""<RuleSet Name="HouseRules {level}" Description="HouseRules {level}" ToolsVersion="17.0">""");
        text.Line("""  <Rules AnalyzerId="HouseRules" RuleNamespace="HouseRules">""");
        foreach (var rule in RulesUpTo(catalogue, level).Where(rule => rule.Fixable))
        {
            text.Line($"""    <Rule Id="{rule.Id}" Action="{RuleSetAction(rule.Severity)}" />""");
        }

        text.Line("  </Rules>");
        text.Line("</RuleSet>");
        return text.ToString();
    }

    private static string RuleSetAction(string severity) => severity switch
    {
        "error" => "Error",
        "warning" => "Warning",
        "suggestion" => "Info",
        "silent" => "Hidden",
        "none" => "None",
        _ => throw new InvalidDataException($"Unknown severity '{severity}'."),
    };

    private static string Props(Catalogue catalogue)
    {
        var text = new Text();
        text.Line("<Project>");
        text.Line();
        text.Line($"  <!-- {GeneratedNote} -->");
        text.Line();
        text.Line("  <!-- Build settings must be defaults set here, before the SDK sets its own defaults. They");
        text.Line("       therefore follow the level known at this point: Directory.Build.props or the command");
        text.Line("       line. -->");
        text.Line("  <PropertyGroup>");
        text.Line(DefaultProperty("HouseRulesLevel", "Recommended"));
        text.Line("    <_HouseRulesLevelAtProps>$(HouseRulesLevel)</_HouseRulesLevelAtProps>");
        text.Line("    <_HouseRulesRank>0</_HouseRulesRank>");
        text.Line("    <!-- Linux file names are case-sensitive: spell the level as its config files do. -->");
        foreach (var level in Levels)
        {
            var when = LevelIs(level);
            text.Line($"""    <_HouseRulesRank Condition="{when}">{Rank(level)}</_HouseRulesRank>""");
            text.Line($"""    <_HouseRulesLevelAtProps Condition="{when}">{level}</_HouseRulesLevelAtProps>""");
        }

        text.Line("    <_HouseRulesConfig>$(MSBuildThisFileDirectory)../config/</_HouseRulesConfig>");
        text.Line("  </PropertyGroup>");
        foreach (var level in Levels)
        {
            text.Line();
            text.Line($"""  <PropertyGroup Condition="'$(_HouseRulesRank)' >= '{Rank(level)}'">""");
            foreach (var property in catalogue.Properties.Where(entry => entry.Level == level))
            {
                text.Line(DefaultProperty(property.Name, property.Value, property.Condition));
            }

            foreach (var category in catalogue.Categories.Where(entry => entry.Level == level))
            {
                text.Line(DefaultProperty("AnalysisMode" + category.Name, category.Mode));
            }

            foreach (var ban in catalogue.Banned.Where(entry => entry.Level == level))
            {
                text.Line(DefaultProperty(ban.Switch, "true"));
            }

            text.Line("  </PropertyGroup>");
        }

        text.Line();
        text.Line("""  <PropertyGroup Condition="'$(_HouseRulesRank)' != '0'">""");
        text.Line(DefaultProperty("CodeAnalysisRuleSet", "$(_HouseRulesConfig)$(_HouseRulesLevelAtProps).ruleset"));
        text.Line("  </PropertyGroup>");
        text.Line();
        text.Line("  <!-- Items are evaluated after every property, so the conditions below see the project's final");
        text.Line("       values. -->");
        text.Line("""  <ItemGroup Condition="'$(_HouseRulesRank)' != '0'">""");
        text.Line(ConfigItem("EditorConfigFiles", "$(_HouseRulesLevelAtProps).globalconfig"));
        text.Line(ConfigItem("EditorConfigFiles", "Tests.globalconfig", TestProject));
        foreach (var ban in catalogue.Banned)
        {
            var file = $"BannedSymbols.{ban.Category}.txt";
            text.Line(ConfigItem("AdditionalFiles", file, $"'$({ban.Switch})' == 'true'"));
        }

        text.Line("  </ItemGroup>");
        var additionalFiles = catalogue.Rules.Where(rule => rule.AdditionalFile is not null)
            .GroupBy(rule => rule.AdditionalFile!)
            .Select(group => (File: group.Key, Rank: group.Min(rule => Rank(rule.Level))));

        foreach (var (file, rank) in additionalFiles)
        {
            text.Line();
            text.Line($"""  <ItemGroup Condition="'$(_HouseRulesRank)' >= '{rank}'">""");
            text.Line(ConfigItem("AdditionalFiles", file));
            text.Line("  </ItemGroup>");
        }

        text.Line();
        text.Line("</Project>");
        return text.ToString();
    }

    private static string LevelIs(string level) => $"'$(HouseRulesLevel)' == '{level}'";

    private static string DefaultProperty(string name, string value, string? condition = null)
    {
        var when = condition is null ? "" : $" and ({condition})";
        return $"""    <{name} Condition="'$({name})' == ''{when}">{value}</{name}>""";
    }

    private static string ConfigItem(string itemType, string file, string? condition = null)
    {
        var when = condition is null ? "" : $""" Condition="{condition}" """.TrimEnd();
        return $"""    <{itemType} Include="$(_HouseRulesConfig){file}"{when} />""";
    }

    private static string LevelsDoc(Catalogue catalogue)
    {
        var text = new Text();
        text.Line("# Levels");
        text.Line();
        text.Line("<!-- " + GeneratedNote + " -->");
        text.Line();
        text.Line("Choose a level with `<HouseRulesLevel>` in `Directory.Build.props`; the default is `Recommended`. "
            + "Each level contains everything in the levels before it, so the tables under a level list "
            + "exactly what moving up to it costs. Generated from [the rules catalogue](../catalogue/rules.json).");

        foreach (var level in Levels)
        {
            text.Line();
            text.Line("## " + level);
            text.Line();
            text.Line(LevelIntro(level));
            var properties = catalogue.Properties.Where(entry => entry.Level == level).ToList();
            if (properties.Count > 0)
            {
                text.Line();
                text.Line("### Build settings");
                text.Line();
                text.Line("| Property | Value | Why |");
                text.Line("|---|---|---|");
                foreach (var property in properties)
                {
                    var condition = property.Condition is null ? "" : $" (when `{property.Condition}`)";
                    text.Line($"| `{property.Name}` | `{property.Value}`{condition} | {property.Description} |");
                }
            }

            var categories = catalogue.Categories.Where(entry => entry.Level == level).ToList();
            if (categories.Count > 0)
            {
                text.Line();
                text.Line("### Code analysis categories");
                text.Line();
                text.Line("| Category | Mode |");
                text.Line("|---|---|");
                foreach (var category in categories)
                {
                    text.Line($"| {category.Name} | `{category.Mode}` |");
                }
            }

            var rules = catalogue.Rules.Where(entry => entry.Level == level).ToList();
            if (rules.Count > 0)
            {
                text.Line();
                text.Line("### Rules");
                text.Line();
                text.Line("| ID | Source | What it enforces | Severity | Fixed by `dotnet format` |");
                text.Line("|---|---|---|---|---|");
                foreach (var rule in rules)
                {
                    var fixable = rule.Fixable ? "yes" : "no";
                    var source = SourceName(rule.Source);
                    var description = rule.Source == "BannedApiAnalyzers"
                        ? $"{rule.Description} {BanTableLinks(catalogue)}"
                        : rule.Description;

                    text.Line($"| {RuleLink(rule)} | {source} | {description} | {rule.Severity} | {fixable} |");
                }
            }

            var bans = catalogue.Banned.Where(entry => entry.Level == level).ToList();
            if (bans.Count > 0)
            {
                text.Line();
                text.Line($"### Banned APIs at {level}");
                text.Line();
                text.Line("| Category | Turn off with | What it bans |");
                text.Line("|---|---|---|");
                foreach (var ban in bans)
                {
                    text.Line($"| {ban.Category} | `<{ban.Switch}>false</{ban.Switch}>` | {ban.Description} |");
                }
            }
        }

        text.Line();
        text.Line("## Test projects");
        text.Line();
        text.Line("Test projects (`IsTestProject` or `IsTestingPlatformApplication` is `true`) also get these "
            + "relaxations (naming and code metrics, never bans):");

        text.Line();
        text.Line("| Setting | Value | Why |");
        text.Line("|---|---|---|");
        foreach (var entry in catalogue.Tests)
        {
            text.Line($"| `{entry.Key}` | `{entry.Value}` | {entry.Description} |");
        }

        return text.ToString();
    }

    private static string LevelIntro(string level) => level switch
    {
        "Baseline" => "Security and reliability rules, plus the layout rules `dotnet format` fixes without judgement. "
            + "Turning it on costs one mechanical commit and the security fixes.",
        "Recommended" => "Adds design, maintainability, naming and usage rules whose fixes need human judgement.",
        _ => "Adds nullable reference types, every compiler warning wave, code metrics, the remaining style rules and "
            + "bans that push a design: injected clocks, randomness, logging, configuration and HTTP clients.",
    };

    private static string SourceName(string source) => source switch
    {
        "IDE" => ".NET SDK code style",
        "CA" => ".NET SDK code analysis",
        "CS" => "C# compiler",
        _ => source,
    };

    private static string BanTableLinks(Catalogue catalogue)
    {
        var links = catalogue.Banned
            .Select(ban => ban.Level)
            .Distinct()
            .Select(level => $"[{level}](#banned-apis-at-{level.ToLowerInvariant()})");

        return $"Categories: {string.Join(", ", links)}.";
    }

    private static string RuleLink(RuleEntry rule) => rule.Source switch
    {
        _ when rule.Docs is not null => $"[{rule.Id}]({rule.Docs})",
        "HouseRules" => $"[{rule.Id}](rules/{rule.Id}.md)",
        "IDE" => $"[{rule.Id}]({CodeAnalysisDocs}style-rules/{Lower(rule.Id)})",
        "CA" => $"[{rule.Id}]({CodeAnalysisDocs}quality-rules/{Lower(rule.Id)})",
        "Roslynator" => $"[{rule.Id}](https://josefpihrt.github.io/docs/roslynator/analyzers/{rule.Id})",
        _ => rule.Id,
    };

    private static string Lower(string id) => id.ToLowerInvariant();

    private sealed class Text
    {
        private readonly StringBuilder _builder = new();

        public void Line(string line = "") => _builder.Append(line).Append('\n');

        public override string ToString() => _builder.ToString();
    }
}
