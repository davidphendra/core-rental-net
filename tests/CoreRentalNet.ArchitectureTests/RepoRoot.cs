namespace CoreRentalNet.ArchitectureTests;

/// <summary>Locates the repository root by walking up from the test output directory.</summary>
internal static class RepoRoot
{
    public static string Path { get; } = Find();

    public static string Combine(params string[] segments)
        => System.IO.Path.Combine([Path, .. segments]);

    private static string Find()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            if (File.Exists(System.IO.Path.Combine(directory.FullName, "CoreRentalNet.sln")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException(
            $"Could not locate CoreRentalNet.sln by walking up from '{AppContext.BaseDirectory}'.");
    }
}
