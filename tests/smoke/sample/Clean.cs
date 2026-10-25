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
