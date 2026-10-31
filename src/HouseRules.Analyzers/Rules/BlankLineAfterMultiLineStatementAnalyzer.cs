using System.Collections.Immutable;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace HouseRules.Analyzers.Rules;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class BlankLineAfterMultiLineStatementAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Descriptors.BlankLineAfterMultiLineStatement);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(
            AnalyzeStatements,
            SyntaxKind.Block,
            SyntaxKind.SwitchSection,
            SyntaxKind.CompilationUnit);
    }

    private static void AnalyzeStatements(SyntaxNodeAnalysisContext context)
    {
        var lines = context.Node.SyntaxTree.GetText(context.CancellationToken).Lines;
        SyntaxNode? previous = null;
        foreach (var statement in StatementsOf(context.Node))
        {
            if (previous is not null && NeedsBlankLineBetween(previous, statement, lines))
            {
                context.ReportDiagnostic(
                    Diagnostic.Create(
                        Descriptors.BlankLineAfterMultiLineStatement,
                        statement.GetFirstToken().GetLocation()));
            }

            previous = statement;
        }
    }

    private static IEnumerable<SyntaxNode> StatementsOf(SyntaxNode node) => node switch
    {
        BlockSyntax block => block.Statements,
        SwitchSectionSyntax section => section.Statements,
        CompilationUnitSyntax unit => unit.Members.OfType<GlobalStatementSyntax>(),
        _ => [],
    };

    // Only the lines between the two statements are inspected, so a directive that closes the previous statement
    // (e.g. `#pragma warning restore`) may sit above the blank line, while a comment never counts as one.
    internal static bool NeedsBlankLineBetween(SyntaxNode previous, SyntaxNode next, TextLineCollection lines)
    {
        var previousSpan = lines.GetLinePositionSpan(previous.Span);
        var nextLine = lines.GetLinePosition(next.SpanStart).Line;
        if (previousSpan.End.Line == previousSpan.Start.Line || nextLine <= previousSpan.End.Line)
        {
            return false;
        }

        for (var line = previousSpan.End.Line + 1; line < nextLine; line++)
        {
            if (string.IsNullOrWhiteSpace(lines[line].ToString()))
            {
                return false;
            }
        }

        return true;
    }
}
