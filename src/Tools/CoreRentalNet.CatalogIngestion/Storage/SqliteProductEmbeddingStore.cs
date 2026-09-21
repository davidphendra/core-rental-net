using System.Globalization;
using CoreRentalNet.BuildingBlocks.Infrastructure.Sqlite;
using Microsoft.Data.Sqlite;

namespace CoreRentalNet.CatalogIngestion.Storage;

/// <summary>The vector table, in the tool's own SQLite file.</summary>
/// <remarks>
/// <para>
/// <b>Raw <c>Microsoft.Data.Sqlite</c>, not an ORM.</b> The table is one shape, written whole and never
/// queried by the tool, so a mapping layer would be machinery with nothing to map.
/// </para>
/// <para>
/// <b>The file's journal mode and busy timeout come from <see cref="SqliteDatabaseSettings"/></b>, the same
/// type the application opens its database with, so the two files are written under the same rules.
/// </para>
/// <para>
/// <b>No runtime value is interpolated into SQL:</b> every value is a parameter, including the blobs. The one
/// substitution is the table name, which is a compile-time constant.
/// </para>
/// </remarks>
internal sealed class SqliteProductEmbeddingStore : IProductEmbeddingStore
{
    /// <summary>The table's name, in one place: the schema and the foreign-file check must agree on it.</summary>
    private const string TableName = "product_embedding";

    private const string CreateTable = $"""
        CREATE TABLE IF NOT EXISTS {TableName} (
            id          TEXT NOT NULL PRIMARY KEY,
            skuNo       TEXT NOT NULL,
            name        TEXT NOT NULL,
            description TEXT NOT NULL,
            embedding   BLOB NOT NULL,
            created_at  TEXT NOT NULL
        );
        """;

    private readonly SqliteDatabaseSettings _settings;

    /// <summary>The store for one database file, which this type creates on first write.</summary>
    public SqliteProductEmbeddingStore(string databasePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(databasePath);

        _settings = SqliteDatabaseSettings.Local(databasePath);
    }

    /// <inheritdoc />
    public void ReplaceAll(IReadOnlyList<ProductEmbeddingRow> rows, DateTimeOffset createdAt)
    {
        ArgumentNullException.ThrowIfNull(rows);

        using var connection = SqliteDatabase.Open(_settings);

        RefuseAForeignFile(connection);
        EnsureTable(connection);

        using var transaction = connection.BeginTransaction();

        DeleteAll(connection, transaction);
        InsertAll(connection, transaction, rows, createdAt);

        transaction.Commit();
    }

    /// <summary>Creates the table when the file is new, and does nothing when it is not.</summary>
    private static void EnsureTable(SqliteConnection connection)
    {
        using var command = connection.CreateCommand();
        command.CommandText = CreateTable;
        command.ExecuteNonQuery();
    }

    /// <summary>Refuses a file that holds any table but this tool's own.</summary>
    /// <remarks>
    /// The store empties its table on every write, so a <c>Database:Path</c> pointed at an unrelated SQLite
    /// file would clear that file's data. Checking the file holds nothing but the tool's table turns that from
    /// silent loss into a refusal that names the table it found.
    /// </remarks>
    private static void RefuseAForeignFile(SqliteConnection connection)
    {
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT name FROM sqlite_master WHERE type = 'table' AND name NOT LIKE 'sqlite%';";

        using var reader = command.ExecuteReader();

        while (reader.Read())
        {
            var table = reader.GetString(0);

            if (!string.Equals(table, TableName, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"The database holds a table named '{table}', which is not this tool's. "
                    + "Refusing to write to a file that is not the tool's own vector database.");
            }
        }
    }

    private static void DeleteAll(SqliteConnection connection, SqliteTransaction transaction)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "DELETE FROM product_embedding;";
        command.ExecuteNonQuery();
    }

    /// <summary>Every row of the run, under one timestamp and one new identity each.</summary>
    private static void InsertAll(
        SqliteConnection connection,
        SqliteTransaction transaction,
        IReadOnlyList<ProductEmbeddingRow> rows,
        DateTimeOffset createdAt)
    {
        var stamp = createdAt.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture);

        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            INSERT INTO product_embedding (id, skuNo, name, description, embedding, created_at)
            VALUES ($id, $skuNo, $name, $description, $embedding, $createdAt);
            """;

        var id = command.Parameters.Add("$id", SqliteType.Text);
        var skuNo = command.Parameters.Add("$skuNo", SqliteType.Text);
        var name = command.Parameters.Add("$name", SqliteType.Text);
        var description = command.Parameters.Add("$description", SqliteType.Text);
        var embedding = command.Parameters.Add("$embedding", SqliteType.Blob);
        var createdAtParameter = command.Parameters.Add("$createdAt", SqliteType.Text);

        foreach (var row in rows)
        {
            id.Value = Guid.NewGuid().ToString("D");
            skuNo.Value = row.SkuNo;
            name.Value = row.Name;
            description.Value = row.Description;
            embedding.Value = VectorBlob.ToBytes(row.Embedding);
            createdAtParameter.Value = stamp;

            command.ExecuteNonQuery();
        }
    }
}
