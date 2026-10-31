using System.Globalization;

using HouseRules.Analyzers.Rules;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;

namespace HouseRules.Tests;

public sealed class Hr0002AnalyzerTests
{
    [Fact]
    public async Task Arguments_sharing_the_line_after_the_parenthesis_are_reported()
    {
        await VerifyAsync("""
            class C
            {
                void M()
                {
                    Call(
                        {|HR0002:1|}, 2, 3);
                }

                void Call(int a, int b, int c) { }
            }
            """);
    }

    [Fact]
    public async Task Parameters_sharing_the_line_after_the_parenthesis_are_reported()
    {
        await VerifyAsync("""
            class C
            {
                void M(
                    {|HR0002:int a|}, int b)
                {
                }
            }
            """);
    }

    [Fact]
    public async Task A_record_primary_constructor_is_reported()
    {
        await VerifyAsync("""
            record R(
                {|HR0002:int A|}, int B);
            """);
    }

    [Fact]
    public async Task Lambda_parameters_are_reported()
    {
        await VerifyAsync("""
            class C
            {
                System.Func<int, int, int> f = (
                    {|HR0002:int a|}, int b) => a + b;
            }
            """);
    }

    [Fact]
    public async Task Bracketed_and_attribute_lists_are_reported()
    {
        await VerifyAsync("""
            [System.AttributeUsage(System.AttributeTargets.All)]
            class AAttribute : System.Attribute
            {
                public AAttribute(int a, int b) { }
            }

            [A(
                {|HR0002:1|}, 2)]
            class C
            {
                int M(int[,] grid) => grid[
                    {|HR0002:0|}, 1];

                int this[
                    {|HR0002:int a|}, int b] => a + b;
            }
            """);
    }

    [Fact]
    public async Task The_message_names_the_kind_of_element_and_the_opening_token()
    {
        await VerifyAsync(
            """
            class C
            {
                void M(
                    {|#0:int a|}, int b)
                {
                }

                int this[
                    {|#1:int a|}, int b] => a + b;
            }
            """,
            new DiagnosticResult(RuleIds.OneElementPerLineOnceWrapped, DiagnosticSeverity.Warning)
                .WithLocation(0)
                .WithArguments("parameter", "("),
            new DiagnosticResult(RuleIds.OneElementPerLineOnceWrapped, DiagnosticSeverity.Warning)
                .WithLocation(1)
                .WithArguments("parameter", "["));
    }

    [Fact]
    public async Task A_list_on_the_line_of_its_parenthesis_is_not_reported()
    {
        await VerifyAsync("""
            class C
            {
                void M(int a, int b)
                {
                    Call(1, 2, 3);
                }

                void Call(int a, int b, int c) { }
            }
            """);
    }

    [Fact]
    public async Task One_element_per_line_is_not_reported()
    {
        await VerifyAsync("""
            class C
            {
                void M(
                    int a,
                    int b)
                {
                    Call(
                        1,
                        2,
                        3);
                }

                void Call(int a, int b, int c) { }
            }
            """);
    }

    [Fact]
    public async Task A_single_element_is_not_reported()
    {
        await VerifyAsync("""
            class C
            {
                void M()
                {
                    Call(
                        1);
                }

                void Call(int a) { }
            }
            """);
    }

    [Fact]
    public async Task A_list_split_part_way_is_not_reported()
    {
        await VerifyAsync("""
            class C
            {
                void M()
                {
                    Call(1,
                        2, 3);
                    Call(
                        1, 2,
                        3);
                }

                void Call(int a, int b, int c) { }
            }
            """);
    }

    [Fact]
    public async Task A_list_with_a_multi_line_element_is_not_reported()
    {
        await VerifyAsync("""
            class C
            {
                void M()
                {
                    Run(
                        1, x =>
                        {
                        });
                }

                void Run(int a, System.Action<int> b) { }
            }
            """);
    }

    [Fact]
    public async Task A_comment_between_elements_is_not_reported()
    {
        await VerifyAsync("""
            class C
            {
                void M()
                {
                    Call(
                        1, /* two */ 2);
                }

                void Call(int a, int b) { }
            }
            """);
    }

    [Fact]
    public void The_descriptor_says_what_to_change_and_links_its_docs_page()
    {
        var descriptor = new OneElementPerLineOnceWrappedAnalyzer().SupportedDiagnostics.Single();

        Assert.Equal(RuleIds.OneElementPerLineOnceWrapped, descriptor.Id);
        Assert.False(descriptor.IsEnabledByDefault);
        Assert.Equal(
            "Put each {0} on its own line: the list breaks after its opening '{1}' ('dotnet format' fixes this)",
            descriptor.MessageFormat.ToString(CultureInfo.InvariantCulture));

        Assert.Equal(
            "https://github.com/Hanif6731/HouseRules/blob/main/docs/rules/HR0002.md",
            descriptor.HelpLinkUri);
    }

    private static Task VerifyAsync(string source, params DiagnosticResult[] expected) =>
        Verify.AnalyzerAsync<OneElementPerLineOnceWrappedAnalyzer>(source, expected);
}
