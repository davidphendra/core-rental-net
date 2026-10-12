namespace CoreRentalNet.CatalogPopulate.Tests;

/// <summary>Finds files in the repository from the test's output directory.</summary>
internal static class RepoPaths
{
    /// <summary>The committed products.json, or null when the repository root cannot be found.</summary>
    public static string? ProductsJson()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            var candidate = Path.Combine(directory.FullName, "src", "shared", "data", "products.json");
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        return null;
    }
}
