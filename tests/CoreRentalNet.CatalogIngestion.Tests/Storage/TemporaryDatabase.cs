using Microsoft.Data.Sqlite;

namespace CoreRentalNet.CatalogIngestion.Tests.Storage;

/// <summary>A throwaway SQLite file in its own directory, deleted afterwards.</summary>
/// <remarks>
/// Every test gets its own, so no test can see another's rows and a developer's real vector file is never
/// touched.
/// </remarks>
internal sealed class TemporaryDatabase : IDisposable
{
    private readonly string _directory;

    public TemporaryDatabase()
    {
        _directory = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"core-rental-ingestion-{Guid.NewGuid():N}");
        Path = System.IO.Path.Combine(_directory, "product_embedding.db");

        // Created here so a test can open the file itself — to seed a foreign table, for instance — without
        // depending on the store having done it first.
        Directory.CreateDirectory(_directory);
    }

    /// <summary>The database file's path. Nothing exists on disk until the first write.</summary>
    public string Path { get; }

    public void Dispose()
    {
        // The provider pools connections, so a pooled one would hold the file open on some platforms.
        SqliteConnection.ClearAllPools();

        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, recursive: true);
        }
    }
}
