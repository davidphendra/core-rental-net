using CoreRentalNet.BuildingBlocks.Infrastructure.Sqlite;
using CoreRentalNet.BuildingBlocks.Infrastructure.Vectors;
using Microsoft.Data.Sqlite;

namespace CoreRentalNet.CatalogIngestion.Tests.Seeding;

/// <summary>A vector file written under the names the store used before it moved into Discovery.</summary>
/// <remarks>
/// The names are written out in full rather than taken from the contract on purpose: this is the file the
/// seed generator is expected to lift, and it is the legacy shape the reader has to recognise.
/// </remarks>
internal sealed class LegacyVectorFile : IDisposable
{
    private readonly string _directory;

    public LegacyVectorFile()
    {
        _directory = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"core-rental-legacy-{Guid.NewGuid():N}");
        Path = System.IO.Path.Combine(_directory, "product_embedding.db");
        Directory.CreateDirectory(_directory);

        using var connection = SqliteDatabase.OpenWithVectors(SqliteDatabaseSettings.Local(Path));
        using var command = connection.CreateCommand();

        command.CommandText = """
            CREATE VIRTUAL TABLE product_embedding USING vec0(
                skuNo        TEXT,
                embedding    float[384] distance_metric=cosine,
                +id          TEXT,
                +name        TEXT,
                +description TEXT,
                +created_at  TEXT
            );

            CREATE TABLE product_embedding_recipe (
                modelId       TEXT NOT NULL,
                width         INTEGER NOT NULL,
                composition   TEXT NOT NULL,
                catalogueHash TEXT NOT NULL,
                created_at    TEXT NOT NULL
            );

            INSERT INTO product_embedding_recipe VALUES (
                'text-embedding-3-small', 384, 'name+description+metadata/1', '7f50', '2026-10-06T03:32:00.6567730+00:00');

            INSERT INTO product_embedding (skuNo, embedding, id, name, description, created_at)
            VALUES ('DSK0001', $embedding, 'id-1', 'A desk', 'A description.', '2026-10-06T03:32:00.6567730+00:00');
            """;
        command.Parameters.Add("$embedding", SqliteType.Blob).Value = Embedding;

        command.ExecuteNonQuery();
    }

    /// <summary>The one vector the file holds, as the store wrote it.</summary>
    public static byte[] Embedding { get; } = VectorBlob.ToBytes([.. Enumerable.Repeat(0.5f, 384)]);

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
