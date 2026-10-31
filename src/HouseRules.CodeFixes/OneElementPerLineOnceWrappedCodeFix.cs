using System.Collections.Immutable;
using System.Composition;

using HouseRules.Analyzers.Rules;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace HouseRules.CodeFixes;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(OneElementPerLineOnceWrappedCodeFix))]
[Shared]
public sealed class OneElementPerLineOnceWrappedCodeFix : CodeFixProvider
{
    private const string Title = "Put each element on its own line";

    public override ImmutableArray<string> FixableDiagnosticIds { get; } =
        ImmutableArray.Create(RuleIds.OneElementPerLineOnceWrapped);

    public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public override Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        foreach (var diagnostic in context.Diagnostics)
        {
            context.RegisterCodeFix(
                CodeAction.Create(
                    Title,
                    cancellationToken => SplitAsync(
                        context.Document,
                        diagnostic.Location.SourceSpan.Start,
                        cancellationToken),
                    equivalenceKey: Title),
                diagnostic);
        }

        return Task.CompletedTask;
    }

    private static async Task<Document> SplitAsync(
        Document document,
        int firstElementStart,
        CancellationToken cancellationToken)
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        var text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);

        // The list opens before its first element; a lambda's own parameter list starts with the element itself.
        var list = root?.FindToken(firstElementStart)
            .Parent
            ?.AncestorsAndSelf()
            .FirstOrDefault(node => node.SpanStart < firstElementStart && IsList(node));

        var changes = list switch
        {
            BaseArgumentListSyntax arguments => Changes(text, arguments.Arguments),
            AttributeArgumentListSyntax arguments => Changes(text, arguments.Arguments),
            BaseParameterListSyntax parameters => Changes(text, parameters.Parameters),
            _ => [],
        };

        return changes.Count == 0 ? document : document.WithText(text.WithChanges(changes));
    }

    private static bool IsList(SyntaxNode node) =>
        node is BaseArgumentListSyntax or AttributeArgumentListSyntax or BaseParameterListSyntax;

    // Each gap after a comma becomes a line break plus the indentation the elements already have. A gap that holds
    // anything but whitespace (the document changed since the diagnostic) leaves the list as it is.
    private static List<TextChange> Changes<TNode>(SourceText text, SeparatedSyntaxList<TNode> elements)
        where TNode : SyntaxNode
    {
        var elementsLine = text.Lines.GetLineFromPosition(elements[0].SpanStart);
        var previousLine = text.Lines[elementsLine.LineNumber - 1];
        var lineBreak = text.ToString(TextSpan.FromBounds(previousLine.End, previousLine.EndIncludingLineBreak));
        var indentation = text.ToString(
            TextSpan.FromBounds(elementsLine.Start, FirstNonWhitespace(text, elementsLine)));

        var changes = new List<TextChange>();
        for (var i = 0; i < elements.Count - 1; i++)
        {
            var gap = TextSpan.FromBounds(elements.GetSeparator(i).Span.End, elements[i + 1].SpanStart);
            if (!string.IsNullOrWhiteSpace(text.ToString(gap))
                || text.Lines.GetLineFromPosition(gap.End) != elementsLine)
            {
                return [];
            }

            changes.Add(new TextChange(gap, lineBreak + indentation));
        }

        return changes;
    }

    private static int FirstNonWhitespace(SourceText text, TextLine line)
    {
        var position = line.Start;
        while (position < line.End && char.IsWhiteSpace(text[position]))
        {
            position++;
        }

        return position;
    }
}
