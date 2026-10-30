using Microsoft.Data.Sqlite;

namespace CoreRentalNet.CatalogIngestion.Tests;

/// <summary>A throwaway catalog.db with the three product shapes the pipeline tests exercise.</summary>
internal sealed class TemporaryCatalogDatabase : IDisposable
{
    private readonly string _directory;

    public TemporaryCatalogDatabase(bool empty = false)
    {
        _directory = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"core-rental-catalog-db-{Guid.NewGuid():N}");
        Path = System.IO.Path.Combine(_directory, "catalog.db");

        Directory.CreateDirectory(_directory);

        using var connection = new SqliteConnection($"Data Source={Path}");
        connection.Open();

        using var create = connection.CreateCommand();
        create.CommandText = """
            CREATE TABLE Catalog_Product (
                Sku TEXT NOT NULL PRIMARY KEY,
                Name TEXT NOT NULL,
                Category INTEGER NOT NULL,
                SubCategory INTEGER NULL,
                Amount TEXT NOT NULL,
                Currency TEXT NOT NULL,
                Description TEXT NOT NULL,
                Image TEXT NOT NULL,
                IsFeatured INTEGER NOT NULL,
                Tags TEXT NOT NULL,
                BestFor TEXT NOT NULL,
                NotFor TEXT NOT NULL,
                Attributes TEXT NOT NULL,
                SortOrder INTEGER NOT NULL);
            """;
        create.ExecuteNonQuery();

        if (empty)
        {
            return;
        }

        using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO Catalog_Product VALUES
                ('DSK0001', 'Desk DSK0001', 2, NULL, '800000', 'USD',
                 'A height-adjustable desk.', '/images/desk.svg', 0,
                 '["desk","standing"]', '["all-day use"]', '["outdoor use"]',
                 '{"type":"sit-stand","colour":"oak"}', 1),
                ('CHA0001', 'Chair CHA0001', 1, NULL, '400000', 'USD',
                 'A mesh-backed chair.', '/images/chair.svg', 0,
                 '["chair","mesh-back"]', '["deep work"]', '[]',
                 '{"back":"mesh"}', 2),
                ('MON0001', 'Monitor MON0001', 3, 4, '300000', 'USD',
                 'A 4K monitor.', '/images/monitor.svg', 0,
                 '["monitor","4k"]', '["coding"]', '[]',
                 '{"resolution":"4K"}', 3);
            """;
        command.ExecuteNonQuery();
    }

    public string Path { get; }

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();

        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, recursive: true);
        }
    }
}
