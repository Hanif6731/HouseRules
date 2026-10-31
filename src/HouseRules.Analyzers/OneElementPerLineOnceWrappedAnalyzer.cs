using System.Collections.Immutable;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace HouseRules.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class OneElementPerLineOnceWrappedAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Descriptors.OneElementPerLineOnceWrapped);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(
            AnalyzeList,
            SyntaxKind.ArgumentList,
            SyntaxKind.BracketedArgumentList,
            SyntaxKind.AttributeArgumentList,
            SyntaxKind.ParameterList,
            SyntaxKind.BracketedParameterList);
    }

    private static void AnalyzeList(SyntaxNodeAnalysisContext context)
    {
        switch (context.Node)
        {
            case BaseArgumentListSyntax list:
                Report(context, list.Arguments, "argument");
                break;
            case AttributeArgumentListSyntax list:
                Report(context, list.Arguments, "argument");
                break;
            case BaseParameterListSyntax list:
                Report(context, list.Parameters, "parameter");
                break;
            default:
                break;
        }
    }

    private static void Report<TNode>(
        SyntaxNodeAnalysisContext context,
        SeparatedSyntaxList<TNode> elements,
        string kind)
        where TNode : SyntaxNode
    {
        var open = context.Node.GetFirstToken();
        if (IsPackedAfterOpening(open, elements))
        {
            context.ReportDiagnostic(
                Diagnostic.Create(
                    Descriptors.OneElementPerLineOnceWrapped,
                    elements[0].GetLocation(),
                    kind,
                    open.Text));
        }
    }

    // True when two or more elements share one line that is not the opening token's line. A list split part-way
    // spans several lines and is RCS0053's to report; a comment between elements has no safe place once they split.
    private static bool IsPackedAfterOpening<TNode>(SyntaxToken open, SeparatedSyntaxList<TNode> elements)
        where TNode : SyntaxNode
    {
        if (elements.Count < 2 || open.SyntaxTree is not { } tree)
        {
            return false;
        }

        var lines = tree.GetText().Lines;
        var line = lines.GetLineFromPosition(elements[0].SpanStart).LineNumber;
        if (line == lines.GetLineFromPosition(open.SpanStart).LineNumber
            || line != lines.GetLineFromPosition(elements[elements.Count - 1].Span.End).LineNumber)
        {
            return false;
        }

        for (var i = 0; i < elements.Count - 1; i++)
        {
            var separator = elements.GetSeparator(i);
            if (!IsWhitespace(elements[i].GetTrailingTrivia())
                || !IsWhitespace(separator.LeadingTrivia)
                || !IsWhitespace(separator.TrailingTrivia)
                || !IsWhitespace(elements[i + 1].GetLeadingTrivia()))
            {
                return false;
            }
        }

        return true;
    }

    private static bool IsWhitespace(SyntaxTriviaList trivia) =>
        trivia.All(item => item.IsKind(SyntaxKind.WhitespaceTrivia));
}
