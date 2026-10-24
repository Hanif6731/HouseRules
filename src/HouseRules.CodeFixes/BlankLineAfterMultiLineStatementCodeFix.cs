using System.Collections.Immutable;
using System.Composition;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;

using HouseRules.Analyzers;

namespace HouseRules.CodeFixes;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(BlankLineAfterMultiLineStatementCodeFix))]
[Shared]
public sealed class BlankLineAfterMultiLineStatementCodeFix : CodeFixProvider
{
    private const string Title = "Add a blank line after the multi-line statement";

    public override ImmutableArray<string> FixableDiagnosticIds { get; } =
        ImmutableArray.Create(RuleIds.BlankLineAfterMultiLineStatement);

    public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public override Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        foreach (var diagnostic in context.Diagnostics)
        {
            context.RegisterCodeFix(
                CodeAction.Create(
                    Title,
                    cancellationToken => AddBlankLineAsync(
                        context.Document,
                        diagnostic.Location.SourceSpan.Start,
                        cancellationToken),
                    equivalenceKey: Title),
                diagnostic);
        }

        return Task.CompletedTask;
    }

    private static async Task<Document> AddBlankLineAsync(
        Document document,
        int position,
        CancellationToken cancellationToken)
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        if (root is null)
        {
            return document;
        }

        var token = root.FindToken(position);
        var leading = token.LeadingTrivia;
        var blankLine = SyntaxFactory.EndOfLine(NewLineOf(token.GetPreviousToken()));
        var fixedToken = token.WithLeadingTrivia(leading.Insert(InsertionIndex(leading), blankLine));
        return document.WithSyntaxRoot(root.ReplaceToken(token, fixedToken));
    }

    // Directives right after the statement belong to it (e.g. `#pragma warning restore`), so the blank line goes
    // after the last of them; anything else (a comment) starts the next paragraph, so the blank line goes above it.
    private static int InsertionIndex(SyntaxTriviaList leading)
    {
        var index = 0;
        for (var i = 0; i < leading.Count; i++)
        {
            var trivia = leading[i];
            if (trivia.IsDirective || trivia.IsKind(SyntaxKind.DisabledTextTrivia))
            {
                index = i + 1;
            }
            else if (!trivia.IsKind(SyntaxKind.WhitespaceTrivia) && !trivia.IsKind(SyntaxKind.EndOfLineTrivia))
            {
                break;
            }
        }

        return index;
    }

    private static string NewLineOf(SyntaxToken token)
    {
        foreach (var trivia in token.TrailingTrivia)
        {
            if (trivia.IsKind(SyntaxKind.EndOfLineTrivia))
            {
                return trivia.ToString();
            }
        }

        return "\n";
    }
}
