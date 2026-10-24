using System.Collections.Immutable;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace HouseRules.Analyzers;

/// <summary>
/// The ratchet: suppresses findings recorded in <c>houserules.baseline</c>, each key at most as often as it is
/// recorded, so existing code can adopt a level while every new finding still fails the build.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class BaselineSuppressor : DiagnosticSuppressor
{
    private const string Justification =
        "Recorded in houserules.baseline. Fix it, then run `dotnet build -p:HouseRulesBaseline=prune`.";

    private static readonly ImmutableDictionary<string, SuppressionDescriptor> Descriptors = LoadDescriptors();

    public override ImmutableArray<SuppressionDescriptor> SupportedSuppressions { get; } =
        Descriptors.Values.ToImmutableArray();

    public override void ReportSuppressions(SuppressionAnalysisContext context)
    {
        var file = context.Options.AdditionalFiles.FirstOrDefault(
            additional => string.Equals(
                Path.GetFileName(additional.Path),
                BaselineKey.FileName,
                StringComparison.OrdinalIgnoreCase));

        var text = file?.GetText(context.CancellationToken);
        if (file is null || text is null)
        {
            return;
        }

        var remaining = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var line in text.Lines)
        {
            var entry = line.ToString().Trim();
            if (entry.Length > 0 && entry[0] != '#')
            {
                remaining[entry] = remaining.TryGetValue(entry, out var count) ? count + 1 : 1;
            }
        }

        var directory = Path.GetDirectoryName(file.Path) ?? "";
        var ordered = context.ReportedDiagnostics
            .Where(diagnostic => diagnostic.Location.SourceTree is not null)
            .OrderBy(diagnostic => diagnostic.Location.SourceTree!.FilePath, StringComparer.Ordinal)
            .ThenBy(diagnostic => diagnostic.Location.SourceSpan.Start);

        foreach (var diagnostic in ordered)
        {
            var key = KeyOf(diagnostic, directory, context.CancellationToken);
            if (remaining.TryGetValue(key, out var count)
                && count > 0
                && Descriptors.TryGetValue(diagnostic.Id, out var descriptor))
            {
                remaining[key] = count - 1;
                context.ReportSuppression(Suppression.Create(descriptor, diagnostic));
            }
        }
    }

    private static string KeyOf(Diagnostic diagnostic, string directory, CancellationToken cancellationToken)
    {
        var tree = diagnostic.Location.SourceTree!;
        var line = diagnostic.Location.GetLineSpan().StartLinePosition.Line;
        var lineText = tree.GetText(cancellationToken).Lines[line].ToString();
        return BaselineKey.Of(BaselineKey.RelativePath(directory, tree.FilePath), diagnostic.Id, lineText);
    }

    // Only ids that can fail a build at some level are listed (generated from the catalogue, the compiler and the SDK
    // analyzers): every listed id costs compile time, and a bounded list keeps it negligible.
    private static ImmutableDictionary<string, SuppressionDescriptor> LoadDescriptors()
    {
        using var stream = typeof(BaselineSuppressor).Assembly.GetManifestResourceStream(
            "HouseRules.Analyzers.SuppressibleIds.txt")!;

        using var reader = new StreamReader(stream);
        var builder = ImmutableDictionary.CreateBuilder<string, SuppressionDescriptor>(StringComparer.Ordinal);
        for (var id = reader.ReadLine(); id is not null; id = reader.ReadLine())
        {
            if (id.Length > 0)
            {
                builder[id] = new SuppressionDescriptor("HRB_" + id, id, Justification);
            }
        }

        return builder.ToImmutable();
    }
}
