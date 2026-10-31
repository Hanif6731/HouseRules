using Microsoft.CodeAnalysis;

namespace HouseRules.Analyzers.Rules;

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

    public static readonly DiagnosticDescriptor OneElementPerLineOnceWrapped = new(
        RuleIds.OneElementPerLineOnceWrapped,
        title: "One element per line once a list wraps",
        messageFormat: "Put each {0} on its own line: the list breaks after its opening '{1}' "
            + "('dotnet format' fixes this)",
        category: "Style",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: false,
        helpLinkUri: HelpLink(RuleIds.OneElementPerLineOnceWrapped));

    private static string HelpLink(string id) =>
        $"https://github.com/Hanif6731/HouseRules/blob/main/docs/rules/{id}.md";
}
