using CoreRentalNet.BuildingBlocks.Infrastructure.Sqlite;
using CoreRentalNet.BuildingBlocks.Infrastructure.Vectors;
using Microsoft.Data.Sqlite;

namespace CoreRentalNet.CatalogIngestion.Tests.Storage;

/// <summary>Reads the vector table back the way a consumer would, with SQL rather than through the tool.</summary>
/// <remarks>
/// <para>
/// A test that asserted what the tool was asked to write would prove the call, not the file. This reads the
/// rows back — <b>naming the table in full rather than from the shared constant</b>, so that a rename on
/// either side of the contract fails here — and decodes the blob with the codec the application reads it with,
/// because that is what a consumer actually does.
/// </para>
/// <para>
/// The vector table is a <c>vec0</c> virtual table, so every connection here loads the extension: without it
/// SQLite does not know the module and the table cannot even be read.
/// </para>
/// <para>
/// The byte order itself is not asserted here: it belongs to the codec's own test, and a reader that used its
/// own decoder would agree with a writer that had changed the format.
/// </para>
/// </remarks>
internal static class StoredRows
{
    public static IReadOnlyList<StoredRow> Read(string databasePath)
    {
        using var connection = Open(databasePath);

        using var command = connection.CreateCommand();
        command.CommandText =
            "SELECT id, skuNo, name, description, embedding, created_at FROM product_embedding;";

        var rows = new List<StoredRow>();

        using var reader = command.ExecuteReader();

        while (reader.Read())
        {
            rows.Add(new StoredRow(
                reader.GetString(0),
                reader.GetString(1),
                reader.GetString(2),
                reader.GetString(3),
                VectorBlob.ToFloats((byte[])reader[4]),
                reader.GetString(5)));
        }

        return rows;
    }

    /// <summary>The SKUs nearest a query, nearest first, as sqlite-vec ranks them.</summary>
    /// <remarks>
    /// The ranking is the extension's, which is the point: the tool stores vectors and the consumer's
    /// <c>MATCH</c> query is what orders them, so the two are asserted against each other rather than against a
    /// cosine written here.
    /// </remarks>
    public static IReadOnlyList<string> Nearest(string databasePath, float[] queryVector)
    {
        using var connection = Open(databasePath);

        using var command = connection.CreateCommand();
        command.CommandText =
            "SELECT skuNo FROM product_embedding WHERE embedding MATCH vec_f32($query) AND k = 10 ORDER BY distance ASC;";
        command.Parameters.Add("$query", SqliteType.Blob).Value = VectorBlob.ToBytes(queryVector);

        var skus = new List<string>();

        using var reader = command.ExecuteReader();

        while (reader.Read())
        {
            skus.Add(reader.GetString(0));
        }

        return skus;
    }

    private static SqliteConnection Open(string databasePath)
        => SqliteDatabase.OpenWithVectors(SqliteDatabaseSettings.Local(databasePath));
}
