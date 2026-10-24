namespace HouseRules.Tests;

internal static class RepoRoot
{
    public static string Path { get; } = Find();

    private static string Find()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null;
            directory = directory.Parent)
        {
            if (File.Exists(System.IO.Path.Combine(directory.FullName, "HouseRules.slnx")))
            {
                return directory.FullName;
            }
        }

        throw new InvalidOperationException("HouseRules.slnx not found above " + AppContext.BaseDirectory);
    }
}
