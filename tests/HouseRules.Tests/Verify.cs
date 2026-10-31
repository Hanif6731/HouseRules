using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing;

namespace HouseRules.Tests;

/// <summary>How every rule is verified: against .NET 8 reference assemblies, cancelled with the test.</summary>
/// <remarks>The test harness enables every diagnostic an analyzer supports, whatever its default.</remarks>
internal static class Verify
{
    public static Task AnalyzerAsync<TAnalyzer>(string source, params DiagnosticResult[] expected)
        where TAnalyzer : DiagnosticAnalyzer, new() =>
        AnalyzerAsync<TAnalyzer>(source, OutputKind.DynamicallyLinkedLibrary, expected);

    public static Task AnalyzerAsync<TAnalyzer>(
        string source,
        OutputKind outputKind,
        params DiagnosticResult[] expected)
        where TAnalyzer : DiagnosticAnalyzer, new()
    {
        var test = new CSharpAnalyzerTest<TAnalyzer, DefaultVerifier>
        {
            TestCode = source,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        };

        test.TestState.OutputKind = outputKind;
        test.ExpectedDiagnostics.AddRange(expected);
        return test.RunAsync(TestContext.Current.CancellationToken);
    }

    public static Task CodeFixAsync<TAnalyzer, TCodeFix>(
        string source,
        string fixedSource,
        OutputKind outputKind = OutputKind.DynamicallyLinkedLibrary)
        where TAnalyzer : DiagnosticAnalyzer, new()
        where TCodeFix : CodeFixProvider, new()
    {
        var test = new CSharpCodeFixTest<TAnalyzer, TCodeFix, DefaultVerifier>
        {
            TestCode = source,
            FixedCode = fixedSource,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        };

        test.TestState.OutputKind = outputKind;
        test.FixedState.OutputKind = outputKind;
        return test.RunAsync(TestContext.Current.CancellationToken);
    }
}
