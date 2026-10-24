using System.Collections.Immutable;
using System.Composition;

using HouseRules.Analyzers;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

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

    // A directive that closes something opened before this trivia (`#pragma warning restore`, `#endregion`, `#endif`,
    // `#else`) belongs to the statement above, and so does everything before it, comments included; the blank line goes
    // after the last such directive. Directives that open something (`#region`, `#if`, `#pragma warning disable`),
    // pairs opened and closed here, and comments after the last closing directive belong to the next statement.
    private static int InsertionIndex(SyntaxTriviaList leading)
    {
        var index = 0;
        var depth = 0;
        for (var i = 0; i < leading.Count; i++)
        {
            switch (DirectionOf(leading[i]))
            {
                case Direction.Opens:
                    depth++;
                    break;
                case Direction.Closes when depth > 0:
                    depth--;
                    break;
                case Direction.Closes or Direction.Continues when depth == 0:
                    index = i + 1;
                    break;
                default:
                    break;
            }
        }

        return index;
    }

    private static Direction DirectionOf(SyntaxTrivia trivia) => trivia.GetStructure() switch
    {
        IfDirectiveTriviaSyntax or RegionDirectiveTriviaSyntax => Direction.Opens,
        EndIfDirectiveTriviaSyntax or EndRegionDirectiveTriviaSyntax => Direction.Closes,
        ElseDirectiveTriviaSyntax or ElifDirectiveTriviaSyntax => Direction.Continues,
        PragmaWarningDirectiveTriviaSyntax pragma => pragma.DisableOrRestoreKeyword.IsKind(SyntaxKind.RestoreKeyword)
            ? Direction.Closes
            : Direction.Opens,
        NullableDirectiveTriviaSyntax nullable => nullable.SettingToken.IsKind(SyntaxKind.RestoreKeyword)
            ? Direction.Closes
            : Direction.Opens,
        _ => Direction.None,
    };

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

    private enum Direction
    {
        None,
        Opens,
        Closes,
        Continues,
    }
}
