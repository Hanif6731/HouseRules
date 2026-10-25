using System.Collections.Immutable;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace HouseRules.Analyzers;

/// <summary>
/// Suppresses IDE0051 (unused private member) on constructors. EF Core, serializers and dependency injection call
/// private constructors through reflection, and a private constructor that blocks instantiation is unused by design,
/// so the finding is wrong for constructors and right for every other member.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class PrivateConstructorSuppressor : DiagnosticSuppressor
{
    private static readonly SuppressionDescriptor Descriptor = new(
        "HRC_IDE0051",
        "IDE0051",
        "A private constructor is called through reflection (EF Core, serializers) or blocks instantiation.");

    public override ImmutableArray<SuppressionDescriptor> SupportedSuppressions { get; } = [Descriptor];

    public override void ReportSuppressions(SuppressionAnalysisContext context)
    {
        foreach (var diagnostic in context.ReportedDiagnostics)
        {
            var node = diagnostic.Location.SourceTree
                ?.GetRoot(context.CancellationToken)
                .FindNode(diagnostic.Location.SourceSpan);

            if (node is ConstructorDeclarationSyntax)
            {
                context.ReportSuppression(Suppression.Create(Descriptor, diagnostic));
            }
        }
    }
}
