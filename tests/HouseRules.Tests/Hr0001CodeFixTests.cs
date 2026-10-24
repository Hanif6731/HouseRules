using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;

using HouseRules.Analyzers;
using HouseRules.CodeFixes;

namespace HouseRules.Tests;

public sealed class Hr0001CodeFixTests
{
    [Fact]
    public async Task The_fix_inserts_a_blank_line_after_the_multi_line_statement()
    {
        await VerifyAsync(
            """
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
            """,
            """
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
    public async Task The_blank_line_goes_above_a_comment()
    {
        await VerifyAsync(
            """
            class C
            {
                void M()
                {
                    Call(
                        1); // trailing
                    // next step
                    {|HR0001:Call|}(2);
                }

                void Call(int value) { }
            }
            """,
            """
            class C
            {
                void M()
                {
                    Call(
                        1); // trailing

                    // next step
                    Call(2);
                }

                void Call(int value) { }
            }
            """);
    }

    [Fact]
    public async Task The_blank_line_goes_after_a_directive_that_closes_the_statement()
    {
        await VerifyAsync(
            """
            class C
            {
                void M()
                {
            #pragma warning disable CS0168
                    Call(
                        1);
            #pragma warning restore CS0168
                    // next step
                    {|HR0001:Call|}(2);
                }

                void Call(int value) { }
            }
            """,
            """
            class C
            {
                void M()
                {
            #pragma warning disable CS0168
                    Call(
                        1);
            #pragma warning restore CS0168

                    // next step
                    Call(2);
                }

                void Call(int value) { }
            }
            """);
    }

    [Fact]
    public async Task Fix_all_repairs_two_multi_line_statements_in_a_row()
    {
        await VerifyAsync(
            """
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
            """,
            """
            class C
            {
                void M(bool flag)
                {
                    if (flag)
                    {
                    }

                    Call(
                        1);

                    Call(2);
                }

                void Call(int value) { }
            }
            """);
    }

    [Fact]
    public async Task Switch_sections_and_top_level_statements_are_fixed_and_then_pass_the_analyzer()
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
            """
            System.Console.WriteLine(
                1);

            System.Console.WriteLine(2);

            static void M(int value)
            {
                switch (value)
                {
                    case 1:
                        System.Console.WriteLine(
                            1);

                        break;
                }
            }
            """,
            OutputKind.ConsoleApplication);
    }

    [Fact]
    public async Task The_fix_keeps_the_file_line_endings()
    {
        await VerifyAsync(
            "class C\r\n{\r\n    void M()\r\n    {\r\n        Call(\r\n            1);\r\n"
            + "        {|HR0001:Call|}(2);\r\n    }\r\n\r\n    void Call(int value) { }\r\n}\r\n",
            "class C\r\n{\r\n    void M()\r\n    {\r\n        Call(\r\n            1);\r\n\r\n"
            + "        Call(2);\r\n    }\r\n\r\n    void Call(int value) { }\r\n}\r\n");
    }

    private static Task VerifyAsync(
        string source,
        string fixedSource,
        OutputKind outputKind = OutputKind.DynamicallyLinkedLibrary)
    {
        var test = new CSharpCodeFixTest<
            BlankLineAfterMultiLineStatementAnalyzer,
            BlankLineAfterMultiLineStatementCodeFix,
            DefaultVerifier>
        {
            TestCode = source,
            FixedCode = fixedSource,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        };

        test.TestState.OutputKind = outputKind;
        test.FixedState.OutputKind = outputKind;
        return test.RunAsync(TestContext.Current.CancellationToken);
    }
}
