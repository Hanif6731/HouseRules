using HouseRules.Analyzers.Rules;
using HouseRules.CodeFixes;

namespace HouseRules.Tests;

public sealed class Hr0002CodeFixTests
{
    [Fact]
    public async Task The_fix_puts_each_argument_on_its_own_line()
    {
        await VerifyAsync(
            """
            class C
            {
                void M()
                {
                    Call(
                        {|HR0002:1|}, 2, 3);
                }

                void Call(int a, int b, int c) { }
            }
            """,
            """
            class C
            {
                void M()
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
    public async Task The_fix_puts_each_parameter_on_its_own_line()
    {
        await VerifyAsync(
            """
            class FromQueryAttribute : System.Attribute { }

            class C
            {
                void M(
                    {|HR0002:[FromQuery] int a|}, [FromQuery] int b)
                {
                }
            }
            """,
            """
            class FromQueryAttribute : System.Attribute { }

            class C
            {
                void M(
                    [FromQuery] int a,
                    [FromQuery] int b)
                {
                }
            }
            """);
    }

    [Fact]
    public async Task The_fix_keeps_the_indentation_of_the_elements_line()
    {
        await VerifyAsync(
            """
            class C
            {
                int M() => Outer(
                    Inner(
                        {|HR0002:1|}, 2));

                int Outer(int a) => a;

                int Inner(int a, int b) => a + b;
            }
            """,
            """
            class C
            {
                int M() => Outer(
                    Inner(
                        1,
                        2));

                int Outer(int a) => a;

                int Inner(int a, int b) => a + b;
            }
            """);
    }

    [Fact]
    public async Task The_fix_keeps_tab_indentation()
    {
        await VerifyAsync(
            "class C\n{\n\tvoid M()\n\t{\n\t\tCall(\n\t\t\t{|HR0002:1|}, 2);\n\t}\n\n"
                + "\tvoid Call(int a, int b) { }\n}\n",
            "class C\n{\n\tvoid M()\n\t{\n\t\tCall(\n\t\t\t1,\n\t\t\t2);\n\t}\n\n"
                + "\tvoid Call(int a, int b) { }\n}\n");
    }

    [Fact]
    public async Task The_fix_keeps_crlf_line_endings()
    {
        await VerifyAsync(
            "class C\r\n{\r\n    void M()\r\n    {\r\n        Call(\r\n"
                + "            {|HR0002:1|}, 2);\r\n    }\r\n\r\n    void Call(int a, int b) { }\r\n}\r\n",
            "class C\r\n{\r\n    void M()\r\n    {\r\n        Call(\r\n"
                + "            1,\r\n            2);\r\n    }\r\n\r\n    void Call(int a, int b) { }\r\n}\r\n");
    }

    [Fact]
    public async Task Only_the_packed_list_is_split_when_an_element_is_a_call()
    {
        await VerifyAsync(
            """
            class C
            {
                int M() => Outer(
                    {|HR0002:Inner(1, 2)|}, 3);

                int Outer(int a, int b) => a + b;

                int Inner(int a, int b) => a + b;
            }
            """,
            """
            class C
            {
                int M() => Outer(
                    Inner(1, 2),
                    3);

                int Outer(int a, int b) => a + b;

                int Inner(int a, int b) => a + b;
            }
            """);
    }

    [Fact]
    public async Task A_lambda_as_the_first_element_keeps_its_own_parameter_list()
    {
        await VerifyAsync(
            """
            class C
            {
                void M()
                {
                    Run(
                        {|HR0002:(a, b) => a + b|}, 2);
                }

                void Run(System.Func<int, int, int> f, int x) { }
            }
            """,
            """
            class C
            {
                void M()
                {
                    Run(
                        (a, b) => a + b,
                        2);
                }

                void Run(System.Func<int, int, int> f, int x) { }
            }
            """);
    }

    [Fact]
    public async Task Fix_all_splits_every_packed_list_in_a_document()
    {
        await VerifyAsync(
            """
            class C
            {
                void M()
                {
                    Call(
                        {|HR0002:1|}, 2);
                    Call(
                        {|HR0002:3|}, 4);
                }

                void Call(int a, int b) { }
            }
            """,
            """
            class C
            {
                void M()
                {
                    Call(
                        1,
                        2);
                    Call(
                        3,
                        4);
                }

                void Call(int a, int b) { }
            }
            """);
    }

    private static Task VerifyAsync(string source, string fixedSource) =>
        Verify.CodeFixAsync<OneElementPerLineOnceWrappedAnalyzer, OneElementPerLineOnceWrappedCodeFix>(
            source,
            fixedSource);
}
