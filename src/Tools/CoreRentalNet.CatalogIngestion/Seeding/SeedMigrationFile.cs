namespace CoreRentalNet.CatalogIngestion.Seeding;

/// <summary>The one SeedProductEmbeddings migration shell EF created, found by name.</summary>
/// <remarks>
/// The writer regenerates the whole file, so this only has to find it: the timestamp, the Designer partial
/// and the model snapshot stay EF's, and the tool never invents a migration identity.
/// </remarks>
internal sealed class SeedMigrationFile
{
    private const string Pattern = "*_SeedProductEmbeddings.cs";

    private SeedMigrationFile(string path) => Path = path;

    /// <summary>The file the writer overwrites.</summary>
    public string Path { get; }

    /// <summary>Finds the shell, or says how to create one.</summary>
    public static SeedMigrationFile In(string migrationsDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(migrationsDirectory);

        if (!Directory.Exists(migrationsDirectory))
        {
            throw new DirectoryNotFoundException($"The migrations directory was not found: '{migrationsDirectory}'.");
        }

        var matches = Directory.GetFiles(migrationsDirectory, Pattern);

        return matches.Length == 1
            ? new SeedMigrationFile(matches[0])
            : throw new InvalidOperationException(
                $"Expected exactly one '{Pattern}' in '{migrationsDirectory}', found {matches.Length}. "
                + "Run 'dotnet ef migrations add SeedProductEmbeddings' first.");
    }
}
