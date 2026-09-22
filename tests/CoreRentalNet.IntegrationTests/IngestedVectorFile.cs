using CoreRentalNet.BuildingBlocks.Infrastructure.Sqlite;
using CoreRentalNet.BuildingBlocks.Infrastructure.Vectors;
using Microsoft.Data.Sqlite;

namespace CoreRentalNet.IntegrationTests;

/// <summary>
/// A file shaped exactly as the ingestion tool writes it, for the readers to be tested against.
/// </summary>
/// <remarks>
/// <para>
/// <b>The table and column names are written out in full rather than taken from the shared contract.</b> That
/// is deliberate: this type is the <i>consumer's</i> statement of what the tool produces, so a rename on the
/// tool's side fails here instead of passing because one constant moved on both sides at once. The blob codec
/// is shared, because the byte order is not a name that can be agreed on differently — it is one format, and
/// its own test pins it.
/// </para>
/// <para>
/// The vector table is a <c>vec0</c> virtual table, declared <c>distance_metric=cosine</c>, exactly as the tool
/// declares it. The extension has to be loaded to create or fill it, which is why every connection here goes
/// through <see cref="SqliteDatabase.OpenWithVectors"/>.
/// </para>
/// <para>
/// A throwaway file in its own directory, so no test can see another's rows and a developer's real vector file
/// is never touched.
/// </para>
/// </remarks>
internal sealed class IngestedVectorFile : IDisposable
{
    private const int Width = 384;

    private readonly string _directory;

    public IngestedVectorFile()
    {
        _directory = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"core-rental-vectors-{Guid.NewGuid():N}");
        Path = System.IO.Path.Combine(_directory, "product_embedding.db");

        Directory.CreateDirectory(_directory);

        Execute($"""
            CREATE VIRTUAL TABLE product_embedding USING vec0(
                skuNo         TEXT,
                embedding     float[{Width}] distance_metric=cosine,
                +id           TEXT,
                +name         TEXT,
                +description  TEXT,
                +created_at   TEXT
            );

            CREATE TABLE product_embedding_recipe (
                modelId       TEXT NOT NULL,
                width         INTEGER NOT NULL,
                composition   TEXT NOT NULL,
                catalogueHash TEXT NOT NULL,
                created_at    TEXT NOT NULL
            );
            """);
    }

    /// <summary>The database file's path.</summary>
    public string Path { get; }

    /// <summary>Adds one stored passage of a product.</summary>
    public IngestedVectorFile WithVector(string sku, float[] embedding)
    {
        using var connection = Connection();
        using var command = connection.CreateCommand();

        command.CommandText =
            "INSERT INTO product_embedding (skuNo, embedding, id, name, description, created_at) "
            + "VALUES ($sku, $embedding, $id, $name, $description, $createdAt);";

        command.Parameters.AddWithValue("$sku", sku);
        command.Parameters.AddWithValue("$embedding", VectorBlob.ToBytes(embedding));
        command.Parameters.AddWithValue("$id", Guid.NewGuid().ToString("D"));
        command.Parameters.AddWithValue("$name", $"{sku} name");
        command.Parameters.AddWithValue("$description", $"{sku} description");
        command.Parameters.AddWithValue("$createdAt", "2026-09-21T00:00:00.0000000+00:00");

        command.ExecuteNonQuery();

        return this;
    }

    /// <summary>Records what built the vectors, the way the tool records it.</summary>
    public IngestedVectorFile WithRecipe(string modelId, int width, string composition, string catalogueHash)
    {
        using var connection = Connection();
        using var command = connection.CreateCommand();

        command.CommandText =
            "INSERT INTO product_embedding_recipe (modelId, width, composition, catalogueHash, created_at) "
            + "VALUES ($modelId, $width, $composition, $catalogueHash, $createdAt);";

        command.Parameters.AddWithValue("$modelId", modelId);
        command.Parameters.AddWithValue("$width", width);
        command.Parameters.AddWithValue("$composition", composition);
        command.Parameters.AddWithValue("$catalogueHash", catalogueHash);
        command.Parameters.AddWithValue("$createdAt", "2026-09-21T00:00:00.0000000+00:00");

        command.ExecuteNonQuery();

        return this;
    }

    public void Dispose()
    {
        // The provider pools connections, so a pooled one would hold the file open on some platforms.
        SqliteConnection.ClearAllPools();

        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, recursive: true);
        }
    }

    private SqliteConnection Connection() => SqliteDatabase.OpenWithVectors(SqliteDatabaseSettings.Local(Path));

    private void Execute(string sql)
    {
        using var connection = Connection();
        using var command = connection.CreateCommand();

        command.CommandText = sql;
        command.ExecuteNonQuery();
    }
}
