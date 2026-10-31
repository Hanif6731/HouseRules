using System.Collections.Immutable;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace HouseRules.Analyzers.Suppressors;

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

    public override ImmutableArray<SuppressionDescriptor> SupportedSuppressions { get; } = [.. Descriptors.Values];

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
            var key = KeyOf(diagnostic, directory, context.Options.AdditionalFiles, context.CancellationToken);
            if (key is not null
                && remaining.TryGetValue(key, out var count)
                && count > 0
                && Descriptors.TryGetValue(diagnostic.Id, out var descriptor))
            {
                remaining[key] = count - 1;
                context.ReportSuppression(Suppression.Create(descriptor, diagnostic));
            }
        }
    }

    // The compiler's error log, and therefore the baseline, names the mapped location of code under a #line
    // directive (Razor's generated code maps to the .razor file). Its text comes from the additional file of that
    // name; without one the finding has no key and stays reported.
    private static string? KeyOf(
        Diagnostic diagnostic,
        string directory,
        ImmutableArray<AdditionalText> additionalFiles,
        CancellationToken cancellationToken)
    {
        var mapped = diagnostic.Location.GetMappedLineSpan();
        var line = mapped.StartLinePosition.Line;
        var text = mapped.HasMappedPath
            ? additionalFiles.FirstOrDefault(file => SamePath(file.Path, mapped.Path))?.GetText(cancellationToken)
            : diagnostic.Location.SourceTree!.GetText(cancellationToken);

        if (text is null || line >= text.Lines.Count)
        {
            return null;
        }

        var path = mapped.HasMappedPath ? mapped.Path : diagnostic.Location.SourceTree!.FilePath;
        return BaselineKey.Of(BaselineKey.RelativePath(directory, path), diagnostic.Id, text.Lines[line].ToString());
    }

    private static bool SamePath(string left, string right) =>
        Path.IsPathRooted(left)
            && Path.IsPathRooted(right)
            && string.Equals(Path.GetFullPath(left), Path.GetFullPath(right), StringComparison.OrdinalIgnoreCase);

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
                builder[id] = new SuppressionDescriptor(BaselineKey.SuppressionIdPrefix + id, id, Justification);
            }
        }

        return builder.ToImmutable();
    }
}
