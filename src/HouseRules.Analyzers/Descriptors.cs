using Microsoft.CodeAnalysis;

namespace HouseRules.Analyzers;

internal static class Descriptors
{
    public static readonly DiagnosticDescriptor BlankLineAfterMultiLineStatement = new(
        RuleIds.BlankLineAfterMultiLineStatement,
        title: "Blank line after a multi-line statement",
        messageFormat: "Add a blank line before this statement: the statement above spans several lines "
            + "('dotnet format' fixes this)",
        category: "Style",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri: HelpLink(RuleIds.BlankLineAfterMultiLineStatement));

    private static string HelpLink(string id) =>
        $"https://github.com/Hanif6731/HouseRules/blob/main/docs/rules/{id}.md";
}
