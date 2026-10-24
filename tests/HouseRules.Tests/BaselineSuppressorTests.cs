using HouseRules.Analyzers;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace HouseRules.Tests;

public sealed class BaselineSuppressorTests
{
    private const string Violation = """
        class C
        {
            void M()
            {
                Call(
                    1);
                Call(2);
            }

            void Call(int value) { }
        }
        """;

    private static readonly string Root = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "baseline-tests"));

    [Fact]
    public async Task A_baselined_finding_is_suppressed()
    {
        var diagnostics = await AnalyzeAsync(Entry("A.cs", "HR0001", "Call(2);"), ("A.cs", Violation));

        Assert.True(diagnostics.Single().IsSuppressed);
    }

    [Fact]
    public async Task A_compiler_warning_can_be_baselined()
    {
        const string Source = """
            class C
            {
                void M()
                {
                    int unused;
                }
            }
            """;

        var diagnostics = await AnalyzeAsync(Entry("A.cs", "CS0168", "int unused;"), ("A.cs", Source));

        Assert.True(diagnostics.Single(diagnostic => diagnostic.Id == "CS0168").IsSuppressed);
    }

    [Fact]
    public async Task A_finding_in_a_file_linked_from_outside_the_project_folder_is_suppressed()
    {
        var diagnostics = await AnalyzeAsync(
            Entry("../shared/Linked.cs", "HR0001", "Call(2);"),
            ("../shared/Linked.cs", Violation));

        Assert.True(diagnostics.Single().IsSuppressed);
    }

    [Fact]
    public async Task The_same_finding_in_another_file_is_still_reported()
    {
        var diagnostics = await AnalyzeAsync(
            Entry("A.cs", "HR0001", "Call(2);"),
            ("A.cs", Violation),
            ("B.cs", Violation.Replace("class C", "class D", StringComparison.Ordinal)));

        Assert.Equal([("A.cs", true), ("B.cs", false)], diagnostics.Select(FileAndSuppression));
    }

    [Fact]
    public async Task A_second_copy_on_an_identical_line_is_still_reported()
    {
        const string Twice = """
            class C
            {
                void M()
                {
                    Call(
                        1);
                    Call(2);
                    Call(
                        1);
                    Call(2);
                }

                void Call(int value) { }
            }
            """;

        var diagnostics = await AnalyzeAsync(Entry("A.cs", "HR0001", "Call(2);"), ("A.cs", Twice));

        Assert.Equal([true, false], diagnostics.Select(diagnostic => diagnostic.IsSuppressed));
    }

    [Fact]
    public async Task A_finding_in_mapped_code_is_keyed_on_the_mapped_file()
    {
        var page = Path.Combine(Root, "Page.razor").Replace('\\', '/');
        var generated = $$"""
            class C
            {
                void M()
                {
            #line 3 "{{page}}"
                    Call(
                        1);
                    Call(2);
            #line default
                }

                void Call(int value) { }
            }
            """;

        var diagnostics = await AnalyzeAsync(
            Entry("Page.razor", "HR0001", "Call(2);"),
            ("Page.cs", generated),
            ("Page.razor", "@code {\n  void M() {\n    Call(\n      1);\n    Call(2);\n  }\n}\n"));

        Assert.True(diagnostics.Single().IsSuppressed);
    }

    [Fact]
    public async Task A_baselined_finding_stays_suppressed_when_lines_are_inserted_above_it()
    {
        var diagnostics = await AnalyzeAsync(
            Entry("A.cs", "HR0001", "Call(2);"),
            ("A.cs", "// one\n// two\n" + Violation));

        Assert.True(diagnostics.Single().IsSuppressed);
    }

    [Fact]
    public void Every_catalogue_rule_and_compiler_warning_can_be_baselined()
    {
        var ids = new BaselineSuppressor().SupportedSuppressions
            .Select(descriptor => descriptor.SuppressedDiagnosticId)
            .ToHashSet();

        Assert.All(Generate.CatalogueGenerator.Load(RepoRoot.Path).Rules, rule => Assert.Contains(rule.Id, ids));
        Assert.Contains("CS0168", ids);
        Assert.Contains("CS8602", ids);
        Assert.Contains("CA1822", ids);
        Assert.Contains("CA5351", ids);
    }

    private static string Entry(string path, string id, string line) => BaselineKey.Of(path, id, line) + "\n";

    private static (string File, bool Suppressed) FileAndSuppression(Diagnostic diagnostic) =>
        (Path.GetFileName(diagnostic.Location.SourceTree!.FilePath), diagnostic.IsSuppressed);

    private static async Task<IReadOnlyList<Diagnostic>> AnalyzeAsync(
        string baseline,
        params (string Path, string Source)[] files)
    {
        // Files other than .cs reach the compiler as additional files, as .razor files do.
        AdditionalText[] additional =
        [
            new InMemoryText(Path.Combine(Root, BaselineKey.FileName), baseline),
            .. files.Where(file => !file.Path.EndsWith(".cs", StringComparison.Ordinal))
                .Select(file => new InMemoryText(Path.Combine(Root, file.Path), file.Source)),
        ];

        var options = new CompilationWithAnalyzersOptions(
            new AnalyzerOptions([.. additional]),
            onAnalyzerException: null,
            concurrentAnalysis: true,
            logAnalyzerExecutionTime: false,
            reportSuppressedDiagnostics: true);

        var diagnostics = await Compile(files)
            .WithAnalyzers([new BlankLineAfterMultiLineStatementAnalyzer(), new BaselineSuppressor()], options)
            .GetAllDiagnosticsAsync(TestContext.Current.CancellationToken);

        return [.. diagnostics.Where(diagnostic => diagnostic.Severity >= DiagnosticSeverity.Warning)
            .OrderBy(diagnostic => diagnostic.Location.SourceTree?.FilePath, StringComparer.Ordinal)
            .ThenBy(diagnostic => diagnostic.Location.SourceSpan.Start)];
    }

    private static CSharpCompilation Compile((string Path, string Source)[] files) =>
        CSharpCompilation.Create(
            "Sample",
            files.Where(file => file.Path.EndsWith(".cs", StringComparison.Ordinal))
                .Select(file => CSharpSyntaxTree.ParseText(file.Source, path: Path.Combine(Root, file.Path))),
            [MetadataReference.CreateFromFile(typeof(object).Assembly.Location)],
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

    private sealed class InMemoryText(string path, string text) : AdditionalText
    {
        public override string Path { get; } = path;

        public override SourceText GetText(CancellationToken cancellationToken = default) => SourceText.From(text);
    }
}
