using System.Buffers.Binary;
using Microsoft.Data.Sqlite;

namespace CoreRentalNet.CatalogIngestion.Tests.Storage;

/// <summary>Reads the table back the way any consumer would, with SQL rather than through the tool.</summary>
/// <remarks>
/// A test that asserted what the tool was asked to write would prove the call, not the file. Reading the rows
/// back — and decoding the blob with the format the table is documented to hold — proves what is actually
/// stored, which is the thing a future searcher depends on.
/// </remarks>
internal static class StoredRows
{
    public static IReadOnlyList<StoredRow> Read(string databasePath)
    {
        using var connection = new SqliteConnection(
            new SqliteConnectionStringBuilder { DataSource = databasePath }.ToString());

        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = "SELECT id, skuNo, name, description, embedding, created_at FROM product_embedding;";

        var rows = new List<StoredRow>();

        using var reader = command.ExecuteReader();

        while (reader.Read())
        {
            rows.Add(new StoredRow(
                reader.GetString(0),
                reader.GetString(1),
                reader.GetString(2),
                reader.GetString(3),
                Decode((byte[])reader[4]),
                reader.GetString(5)));
        }

        return rows;
    }

    /// <summary>The floats a stored blob holds: little-endian IEEE-754, the format the tool writes.</summary>
    /// <remarks>
    /// Decoded here rather than through the tool, because the tool only writes and has no decoder. That is the
    /// point: a consumer reads the table with the format it was told to expect, so the test does the same.
    /// </remarks>
    private static float[] Decode(byte[] bytes)
    {
        var values = new float[bytes.Length / sizeof(float)];

        for (var index = 0; index < values.Length; index++)
        {
            values[index] = BinaryPrimitives.ReadSingleLittleEndian(bytes.AsSpan(index * sizeof(float)));
        }

        return values;
    }
}
