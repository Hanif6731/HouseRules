using System.Globalization;

using HouseRules.Analyzers.Rules;

using Microsoft.CodeAnalysis;

namespace HouseRules.Tests;

public sealed class Hr0001AnalyzerTests
{
    [Fact]
    public async Task A_statement_right_after_a_multi_line_one_is_reported()
    {
        await VerifyAsync("""
            class C
            {
                void M()
                {
                    Call(
                        1);
                    {|HR0001:Call|}(2);
                }

                void Call(int value) { }
            }
            """);
    }

    [Fact]
    public async Task Two_multi_line_statements_in_a_row_are_both_reported()
    {
        await VerifyAsync("""
            class C
            {
                void M(bool flag)
                {
                    if (flag)
                    {
                    }
                    {|HR0001:Call|}(
                        1);
                    {|HR0001:Call|}(2);
                }

                void Call(int value) { }
            }
            """);
    }

    [Fact]
    public async Task A_blank_line_after_the_multi_line_statement_satisfies_the_rule()
    {
        await VerifyAsync("""
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
            """);
    }

    [Fact]
    public async Task A_one_line_statement_may_lead_straight_into_a_multi_line_one()
    {
        await VerifyAsync("""
            class C
            {
                void M()
                {
                    var value = 1;
                    Call(
                        value);
                }

                void Call(int value) { }
            }
            """);
    }

    [Fact]
    public async Task A_comment_does_not_replace_the_blank_line()
    {
        await VerifyAsync("""
            class C
            {
                void M()
                {
                    Call(
                        1);
                    // next step
                    {|HR0001:Call|}(2);
                }

                void Call(int value) { }
            }
            """);
    }

    [Fact]
    public async Task A_directive_closing_the_statement_may_sit_before_the_blank_line()
    {
        await VerifyAsync("""
            class C
            {
                void M()
                {
            #pragma warning disable CS0168
                    Call(
                        1);
            #pragma warning restore CS0168

                    Call(2);
                }

                void Call(int value) { }
            }
            """);
    }

    [Theory]
    [InlineData("#pragma warning disable CS0168", "#pragma warning restore CS0168")]
    [InlineData("#region Next", "#endregion")]
    [InlineData("#if true", "#endif")]
    public async Task A_directive_that_opens_the_next_statement_does_not_replace_the_blank_line(
        string open,
        string close)
    {
        await VerifyAsync(
            $$"""
            class C
            {
                void M()
                {
                    Call(
                        1);
            {{open}}
                    {|HR0001:Call|}(2);
            {{close}}
                    Call(
                        3);

            {{open}}
                    Call(4);
            {{close}}
                }

                void Call(int value) { }
            }
            """);
    }

    [Fact]
    public async Task A_comment_above_the_closing_directive_may_sit_before_the_blank_line()
    {
        await VerifyAsync("""
            class C
            {
                void M()
                {
            #pragma warning disable CS0168
                    Call(
                        1);
                    // still covered
            #pragma warning restore CS0168
                    {|HR0001:Call|}(2);
            #pragma warning disable CS0168
                    Call(
                        3);
                    // still covered
            #pragma warning restore CS0168

                    Call(4);
                }

                void Call(int value) { }
            }
            """);
    }

    [Fact]
    public async Task Switch_sections_and_top_level_statements_follow_the_same_rule()
    {
        await VerifyAsync(
            """
            System.Console.WriteLine(
                1);
            {|HR0001:System|}.Console.WriteLine(2);

            static void M(int value)
            {
                switch (value)
                {
                    case 1:
                        System.Console.WriteLine(
                            1);
                        {|HR0001:break|};
                }
            }
            """,
            OutputKind.ConsoleApplication);
    }

    [Fact]
    public async Task The_message_says_exactly_what_to_change()
    {
        var descriptor = new BlankLineAfterMultiLineStatementAnalyzer().SupportedDiagnostics.Single();

        Assert.Equal(RuleIds.BlankLineAfterMultiLineStatement, descriptor.Id);
        Assert.Equal(
            "Add a blank line before this statement: the statement above spans several lines "
                + "('dotnet format' fixes this)",
            descriptor.MessageFormat.ToString(CultureInfo.InvariantCulture));

        Assert.Equal(
            "https://github.com/Hanif6731/HouseRules/blob/main/docs/rules/HR0001.md",
            descriptor.HelpLinkUri);
    }

    private static Task VerifyAsync(string source, OutputKind outputKind = OutputKind.DynamicallyLinkedLibrary) =>
        Verify.AnalyzerAsync<BlankLineAfterMultiLineStatementAnalyzer>(source, outputKind);
}
