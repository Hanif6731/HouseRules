namespace Sample;

public static class Calculator
{
    public static int Add(int left, int right)
    {
        var sum = left
            + right;

        return sum;
    }
}

public sealed class Order
{
    // EF Core and serializers call a private constructor through reflection: IDE0051 must not report it.
    private Order(int id)
    {
        Id = id;
    }

    public int Id { get; }
}

public sealed class OrdersController
{
    private readonly int _pageSize = 20;

    // ASP.NET Core drops the Async suffix from action names, so actions go without it.
    public async Task<int> Get()
    {
        await Task.Yield();

        return _pageSize;
    }
}

public static class NumberNames
{
    // Cyclomatic complexity 20: over the old limit of 15, within the .NET default of 25 that Strict uses.
    public static string Name(int value)
    {
        switch (value)
        {
            case 0:
                return "zero";
            case 1:
                return "one";
            case 2:
                return "two";
            case 3:
                return "three";
            case 4:
                return "four";
            case 5:
                return "five";
            case 6:
                return "six";
            case 7:
                return "seven";
            case 8:
                return "eight";
            case 9:
                return "nine";
            case 10:
                return "ten";
            case 11:
                return "eleven";
            case 12:
                return "twelve";
            case 13:
                return "thirteen";
            case 14:
                return "fourteen";
            case 15:
                return "fifteen";
            case 16:
                return "sixteen";
            case 17:
                return "seventeen";
            default:
                return "many";
        }
    }
}
