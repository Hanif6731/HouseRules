namespace Sample;

public static class Legacy
{
    [Obsolete("Call Current instead.", DiagnosticId = "LEGACY001")]
    public static int Old() => 1;

    public static int Current() => Old();
}
