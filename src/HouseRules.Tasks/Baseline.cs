using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;

using HouseRules.Analyzers;

namespace HouseRules.Tasks;

/// <summary>A diagnostic from the compiler's error log, at a 1-based line.</summary>
public sealed record Finding(string Path, int Line, string RuleId, bool Suppressed);

/// <summary>Reads, writes and updates <c>houserules.baseline</c>: one key per recorded finding.</summary>
public static class Baseline
{
    private const string Header =
        "# Findings recorded by HouseRules (path|rule|line hash). Fix them, then run "
            + "`dotnet build -p:HouseRulesBaseline=prune`; this file only ever shrinks.";

    public static Dictionary<string, int> Parse(string text)
    {
        var entries = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var raw in text.Split('\n'))
        {
            var line = raw.Trim();
            if (line.Length > 0 && line[0] != '#')
            {
                entries[line] = entries.TryGetValue(line, out var count) ? count + 1 : 1;
            }
        }

        return entries;
    }

    public static string Format(IReadOnlyDictionary<string, int> entries)
    {
        var text = new StringBuilder(Header).Append('\n');
        foreach (var entry in entries.OrderBy(pair => pair.Key, StringComparer.Ordinal))
        {
            for (var i = 0; i < entry.Value; i++)
            {
                _ = text.Append(entry.Key).Append('\n');
            }
        }

        return text.ToString();
    }

    /// <summary>Every finding the build reported that is not already suppressed in source.</summary>
    public static Dictionary<string, int> Init(
        IEnumerable<Finding> findings,
        string baselineDirectory,
        Func<string, IReadOnlyList<string>> readLines)
    {
        return Count(findings.Where(finding => !finding.Suppressed), baselineDirectory, readLines);
    }

    /// <summary>
    /// Keeps each recorded entry only as often as it still occurs. Entries are never added, so the baseline only
    /// shrinks.
    /// </summary>
    public static Dictionary<string, int> Prune(
        IReadOnlyDictionary<string, int> recorded,
        IEnumerable<Finding> findings,
        string baselineDirectory,
        Func<string, IReadOnlyList<string>> readLines)
    {
        var current = Count(findings, baselineDirectory, readLines);
        var pruned = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var entry in recorded)
        {
            var still = current.TryGetValue(entry.Key, out var count) ? Math.Min(count, entry.Value) : 0;
            if (still > 0)
            {
                pruned[entry.Key] = still;
            }
        }

        return pruned;
    }

    /// <summary>Findings with a source location from a SARIF 2.1 error log written by the compiler.</summary>
    public static IReadOnlyList<Finding> ReadErrorLog(string sarif)
    {
        var serializer = new DataContractJsonSerializer(typeof(SarifLog));
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(sarif));
        var log = (SarifLog?)serializer.ReadObject(stream);
        var findings = new List<Finding>();
        foreach (var result in (log?.Runs ?? []).SelectMany(run => run.Results ?? []))
        {
            var location = result.Locations?.FirstOrDefault()?.PhysicalLocation;
            if (result.RuleId is null || location?.ArtifactLocation?.Uri is not { } uri || location.Region is null)
            {
                continue;
            }

            var suppressed = result.Suppressions is { Length: > 0 };
            findings.Add(new Finding(new Uri(uri).LocalPath, location.Region.StartLine, result.RuleId, suppressed));
        }

        return findings;
    }

    private static Dictionary<string, int> Count(
        IEnumerable<Finding> findings,
        string baselineDirectory,
        Func<string, IReadOnlyList<string>> readLines)
    {
        var lines = new Dictionary<string, IReadOnlyList<string>>(StringComparer.OrdinalIgnoreCase);
        var counts = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var finding in findings)
        {
            if (!lines.TryGetValue(finding.Path, out var fileLines))
            {
                fileLines = readLines(finding.Path);
                lines[finding.Path] = fileLines;
            }

            var text = finding.Line >= 1 && finding.Line <= fileLines.Count ? fileLines[finding.Line - 1] : "";
            var key = BaselineKey.Of(BaselineKey.RelativePath(baselineDirectory, finding.Path), finding.RuleId, text);
            counts[key] = counts.TryGetValue(key, out var count) ? count + 1 : 1;
        }

        return counts;
    }

    [DataContract]
    private sealed class SarifLog
    {
        [DataMember(Name = "runs")]
        public SarifRun[]? Runs { get; set; }
    }

    [DataContract]
    private sealed class SarifRun
    {
        [DataMember(Name = "results")]
        public SarifResult[]? Results { get; set; }
    }

    [DataContract]
    private sealed class SarifResult
    {
        [DataMember(Name = "ruleId")]
        public string? RuleId { get; set; }

        [DataMember(Name = "locations")]
        public SarifLocation[]? Locations { get; set; }

        [DataMember(Name = "suppressions")]
        public SarifSuppression[]? Suppressions { get; set; }
    }

    [DataContract]
    private sealed class SarifLocation
    {
        [DataMember(Name = "physicalLocation")]
        public SarifPhysicalLocation? PhysicalLocation { get; set; }
    }

    [DataContract]
    private sealed class SarifPhysicalLocation
    {
        [DataMember(Name = "artifactLocation")]
        public SarifArtifactLocation? ArtifactLocation { get; set; }

        [DataMember(Name = "region")]
        public SarifRegion? Region { get; set; }
    }

    [DataContract]
    private sealed class SarifArtifactLocation
    {
        [DataMember(Name = "uri")]
        public string? Uri { get; set; }
    }

    [DataContract]
    private sealed class SarifRegion
    {
        [DataMember(Name = "startLine")]
        public int StartLine { get; set; }
    }

    [DataContract]
    private sealed class SarifSuppression
    {
        [DataMember(Name = "kind")]
        public string? Kind { get; set; }
    }
}
