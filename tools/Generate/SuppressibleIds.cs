using System.Globalization;
using System.Reflection;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

namespace HouseRules.Generate;

/// <summary>Every diagnostic id the ratchet must be able to suppress: whatever can fail a build at some level.</summary>
internal static class SuppressibleIds
{
    public static IReadOnlyList<string> All(Catalogue catalogue) =>
        [.. catalogue.Rules.Select(rule => rule.Id)
            .Concat(CompilerWarnings())
            .Concat(NetAnalyzerRules())
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)];

    // Roslyn names its warning codes WRN_*. The CS9xxx block is added whole, so warnings from compilers newer than
    // the 5.0 referenced here can be recorded too.
    private static IEnumerable<string> CompilerWarnings()
    {
        var errorCode = typeof(CSharpCompilation).Assembly.GetType("Microsoft.CodeAnalysis.CSharp.ErrorCode", true)!;
        var known = Enum.GetNames(errorCode)
            .Where(name => name.StartsWith("WRN_", StringComparison.Ordinal))
            .Select(name => (int)Enum.Parse(errorCode, name));

        return known.Concat(Enumerable.Range(9000, 1000))
            .Select(code => "CS" + code.ToString("D4", CultureInfo.InvariantCulture));
    }

    // The SDK analyzers of the AnalysisLevel the package pins; their rule set is what the category modes enable.
    private static IEnumerable<string> NetAnalyzerRules()
    {
        var root = typeof(SuppressibleIds).Assembly.GetCustomAttributes<AssemblyMetadataAttribute>()
            .Single(attribute => attribute.Key == "NetAnalyzersPath")
            .Value!;

        // The language-neutral assembly sits in analyzers/dotnet, the C# one in analyzers/dotnet/cs.
        var shared = Path.Combine(root, "analyzers", "dotnet");
        return Directory.GetFiles(shared, "*.dll")
            .Concat(Directory.GetFiles(Path.Combine(shared, "cs"), "*.dll"))
            .SelectMany(file => AnalyzersIn(Assembly.LoadFrom(file)))
            .SelectMany(analyzer => analyzer.SupportedDiagnostics)
            .Select(descriptor => descriptor.Id);
    }

    private static IEnumerable<DiagnosticAnalyzer> AnalyzersIn(Assembly assembly)
    {
        Type?[] types;
        try
        {
            types = assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException exception)
        {
            types = exception.Types;
        }

        return types.OfType<Type>()
            .Where(type => !type.IsAbstract
                && typeof(DiagnosticAnalyzer).IsAssignableFrom(type)
                && type.GetCustomAttribute<DiagnosticAnalyzerAttribute>() is not null)
            .Select(type => (DiagnosticAnalyzer)Activator.CreateInstance(type)!);
    }
}
