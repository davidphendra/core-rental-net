using System.Globalization;
using CoreRentalNet.BuildingBlocks.Application.Embeddings;
using CoreRentalNet.BuildingBlocks.Infrastructure.Sqlite;
using CoreRentalNet.BuildingBlocks.Infrastructure.Vectors;
using Microsoft.Data.Sqlite;

namespace CoreRentalNet.CatalogIngestion.Storage;

/// <summary>The vector table and the recipe that describes it, in the tool's own SQLite file.</summary>
/// <remarks>
/// <para>
/// <b>Raw <c>Microsoft.Data.Sqlite</c>, not an ORM.</b> The tables are written whole and never queried by the
/// tool, so a mapping layer would be machinery with nothing to map.
/// </para>
/// <para>
/// <b>The vector table is a <c>vec0</c> virtual table.</b> The application searches it with <c>MATCH</c>, which
/// only a vec0 table answers, so the table is declared <c>distance_metric=cosine</c> — vec0's default is L2 —
/// and its vectors are the ingestion model's own little-endian floats. The non-vector columns are declared in
/// the tier vec0 gives them: the short identifier that is grouped by is a metadata column, and the long or
/// merely-informational ones are auxiliary, which is what vec0 wants for values that never appear in a
/// <c>WHERE</c>.
/// </para>
/// <para>
/// <b>The file's journal mode and busy timeout come from <see cref="SqliteDatabaseSettings"/></b>, the same
/// type the application opens its database with, so both files are written under the same rules.
/// </para>
/// <para>
/// <b>No runtime value is interpolated into SQL:</b> every value is a parameter, including the blobs. The only
/// substitutions are the table names, which are compile-time constants shared with the reader
/// (<see cref="ProductVectorContract"/>), and the vector width, which is the recipe's own.
/// </para>
/// </remarks>
internal sealed class SqliteProductEmbeddingStore : IProductEmbeddingStore
{
    private readonly SqliteDatabaseSettings _settings;

    /// <summary>The store for one database file, which this type creates on first write.</summary>
    public SqliteProductEmbeddingStore(string databasePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(databasePath);

        _settings = SqliteDatabaseSettings.Local(databasePath);
    }

    /// <inheritdoc />
    public void ReplaceAll(IReadOnlyList<ProductEmbeddingRow> rows, EmbeddingRecipe recipe, DateTimeOffset createdAt)
    {
        ArgumentNullException.ThrowIfNull(rows);
        ArgumentNullException.ThrowIfNull(recipe);

        using var connection = SqliteDatabase.OpenWithVectors(_settings);

        RefuseAForeignFile(connection);

        using var transaction = connection.BeginTransaction();

        // The vector table is dropped and rebuilt rather than emptied: vec0 fixes its width at creation, so a
        // re-ingestion at another width has to be a new table. Everything below happens in one transaction, so
        // a failure leaves the previous run's table in place.
        ReplaceVectorTable(connection, transaction, recipe.Width);
        DeleteRecipe(connection, transaction);

        var stamp = createdAt.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture);

        InsertVectors(connection, transaction, rows, stamp);
        InsertRecipe(connection, transaction, recipe, stamp);

        transaction.Commit();
    }

    /// <summary>The vec0 vector table, at the recipe's width, and the recipe table beside it.</summary>
    private static void ReplaceVectorTable(SqliteConnection connection, SqliteTransaction transaction, int width)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = $"""
            DROP TABLE IF EXISTS {ProductVectorContract.VectorTable};

            CREATE VIRTUAL TABLE {ProductVectorContract.VectorTable} USING vec0(
                skuNo         TEXT,
                embedding     float[{width}] distance_metric=cosine,
                +id           TEXT,
                +name         TEXT,
                +description  TEXT,
                +created_at   TEXT
            );

            CREATE TABLE IF NOT EXISTS {ProductVectorContract.RecipeTable} (
                modelId       TEXT NOT NULL,
                width         INTEGER NOT NULL,
                composition   TEXT NOT NULL,
                catalogueHash TEXT NOT NULL,
                created_at    TEXT NOT NULL
            );
            """;
        command.ExecuteNonQuery();
    }

    /// <summary>Refuses a file that holds any table but this tool's own.</summary>
    /// <remarks>
    /// The store replaces its tables on every write, so a <c>Database:Path</c> pointed at an unrelated SQLite
    /// file would clear that file's data. Checking the file holds nothing but the tool's tables turns that from
    /// silent loss into a refusal that names the table it found. A vec0 table is a family — the virtual table,
    /// the recipe, and the shadow tables vec0 creates under the vector table's name — so the whole family is
    /// accepted together.
    /// </remarks>
    private static void RefuseAForeignFile(SqliteConnection connection)
    {
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT name FROM sqlite_master WHERE type = 'table' AND name NOT LIKE 'sqlite%';";

        using var reader = command.ExecuteReader();

        while (reader.Read())
        {
            var table = reader.GetString(0);

            if (!IsOurs(table))
            {
                throw new InvalidOperationException(
                    $"The database holds a table named '{table}', which is not this tool's. "
                    + "Refusing to write to a file that is not the tool's own vector database.");
            }
        }
    }

    /// <summary>Whether a table in the file belongs to this tool's vector table family or its recipe.</summary>
    private static bool IsOurs(string table)
        => string.Equals(table, ProductVectorContract.VectorTable, StringComparison.Ordinal)
            || string.Equals(table, ProductVectorContract.RecipeTable, StringComparison.Ordinal)
            || table.StartsWith(ProductVectorContract.VectorTable + "_", StringComparison.Ordinal);

    /// <summary>Empties the recipe, so the write replaces whatever the previous run left.</summary>
    private static void DeleteRecipe(SqliteConnection connection, SqliteTransaction transaction)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = $"DELETE FROM {ProductVectorContract.RecipeTable};";
        command.ExecuteNonQuery();
    }

    /// <summary>Every row of the run, under one timestamp and one new identity each.</summary>
    private static void InsertVectors(
        SqliteConnection connection,
        SqliteTransaction transaction,
        IReadOnlyList<ProductEmbeddingRow> rows,
        string stamp)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = $"""
            INSERT INTO {ProductVectorContract.VectorTable} (skuNo, embedding, id, name, description, created_at)
            VALUES ($skuNo, $embedding, $id, $name, $description, $createdAt);
            """;

        var skuNo = command.Parameters.Add("$skuNo", SqliteType.Text);
        var embedding = command.Parameters.Add("$embedding", SqliteType.Blob);
        var id = command.Parameters.Add("$id", SqliteType.Text);
        var name = command.Parameters.Add("$name", SqliteType.Text);
        var description = command.Parameters.Add("$description", SqliteType.Text);
        var createdAt = command.Parameters.Add("$createdAt", SqliteType.Text);

        foreach (var row in rows)
        {
            skuNo.Value = row.SkuNo;
            embedding.Value = VectorBlob.ToBytes(row.Embedding);
            id.Value = Guid.NewGuid().ToString("D");
            name.Value = row.Name;
            description.Value = row.Description;
            createdAt.Value = stamp;

            command.ExecuteNonQuery();
        }
    }

    /// <summary>The one row that says what built the vectors written beside it.</summary>
    private static void InsertRecipe(
        SqliteConnection connection,
        SqliteTransaction transaction,
        EmbeddingRecipe recipe,
        string stamp)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = $"""
            INSERT INTO {ProductVectorContract.RecipeTable} (modelId, width, composition, catalogueHash, created_at)
            VALUES ($modelId, $width, $composition, $catalogueHash, $createdAt);
            """;

        command.Parameters.AddWithValue("$modelId", recipe.ModelId);
        command.Parameters.AddWithValue("$width", recipe.Width);
        command.Parameters.AddWithValue("$composition", recipe.Composition);
        command.Parameters.AddWithValue("$catalogueHash", recipe.CatalogueHash);
        command.Parameters.AddWithValue("$createdAt", stamp);

        command.ExecuteNonQuery();
    }
}
