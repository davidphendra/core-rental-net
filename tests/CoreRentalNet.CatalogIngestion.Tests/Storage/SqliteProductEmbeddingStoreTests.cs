using System.Globalization;
using AwesomeAssertions;
using CoreRentalNet.CatalogIngestion.Storage;
using Microsoft.Data.Sqlite;
using Xunit;

namespace CoreRentalNet.CatalogIngestion.Tests.Storage;

/// <summary>
/// The vector table in its own SQLite file, written and read back the way a consumer would.
/// </summary>
/// <remarks>
/// Every test reads the rows back with SQL rather than asserting what the tool was asked to write, because
/// what a future searcher depends on is the file, not the call.
/// </remarks>
public sealed class SqliteProductEmbeddingStoreTests
{
    private static readonly DateTimeOffset Stamp = new(2026, 9, 21, 12, 0, 0, TimeSpan.Zero);

    private static ProductEmbeddingRow Row(string sku, params float[] vector)
        => new(sku, $"{sku} name", $"{sku} description", vector);

    private static IProductEmbeddingStore Store(TemporaryDatabase database)
        => new SqliteProductEmbeddingStore(database.Path);

    [Fact]
    public void The_first_write_creates_the_table_and_holds_every_row()
    {
        using var database = new TemporaryDatabase();

        Store(database).ReplaceAll([Row("A", 1f, 0f), Row("A", 0f, 1f), Row("B", 1f, 1f)], Stamp);

        var rows = StoredRows.Read(database.Path);

        rows.Should().HaveCount(3);
        rows.Select(row => row.SkuNo).Distinct().Should().BeEquivalentTo(["A", "B"]);
        rows.Should().OnlyContain(row => row.Name == $"{row.SkuNo} name");
        rows.Should().OnlyContain(row => row.Description == $"{row.SkuNo} description");
    }

    [Fact]
    public void A_second_write_replaces_the_first_rather_than_appending()
    {
        using var database = new TemporaryDatabase();
        var store = Store(database);

        store.ReplaceAll([Row("A", 1f, 0f), Row("B", 1f, 0f)], Stamp);
        store.ReplaceAll([Row("C", 1f, 0f)], Stamp);

        StoredRows.Read(database.Path).Should().ContainSingle().Which.SkuNo.Should().Be("C");
    }

    [Fact]
    public void Every_row_gets_its_own_id_and_the_run_s_timestamp()
    {
        using var database = new TemporaryDatabase();

        Store(database).ReplaceAll([Row("A", 1f, 0f), Row("A", 0f, 1f)], Stamp);

        var rows = StoredRows.Read(database.Path);
        var expected = Stamp.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture);

        rows.Select(row => row.Id).Should().OnlyHaveUniqueItems();
        rows.Should().OnlyContain(row => row.CreatedAt == expected);
    }

    [Fact]
    public void The_stored_vector_decodes_to_the_floats_that_were_written()
    {
        using var database = new TemporaryDatabase();

        Store(database).ReplaceAll([Row("A", 0.5f, -1.25f, 3f)], Stamp);

        StoredRows.Read(database.Path).Single().Embedding.Should().Equal(0.5f, -1.25f, 3f);
    }

    [Fact]
    public void The_nearest_row_to_a_query_is_the_one_whose_vector_is_closest()
    {
        // The search the tool does not have: a consumer reads the rows and ranks them itself. It is asserted
        // here because it is the property the whole table exists for.
        using var database = new TemporaryDatabase();

        Store(database).ReplaceAll(
            [Row("ALIGNED", 1f, 0f), Row("SIDEWAYS", 0f, 1f), Row("DIAGONAL", 0.7f, 0.7f)],
            Stamp);

        float[] query = [1f, 0f];

        var ranked = StoredRows.Read(database.Path)
            .OrderByDescending(row => Cosine(query, row.Embedding))
            .Select(row => row.SkuNo)
            .ToArray();

        ranked.Should().Equal("ALIGNED", "DIAGONAL", "SIDEWAYS");
    }

    [Fact]
    public void A_database_holding_a_foreign_table_is_refused_rather_than_emptied()
    {
        // A Database:Path pointed at an unrelated SQLite file would otherwise have its rows deleted, silently.
        using var database = new TemporaryDatabase();
        CreateForeignDatabase(database.Path);

        var act = () => Store(database).ReplaceAll([Row("A", 1f, 0f)], Stamp);

        act.Should().Throw<InvalidOperationException>().WithMessage("*not this tool's*");
    }

    private static void CreateForeignDatabase(string databasePath)
    {
        using var connection = new SqliteConnection(
            new SqliteConnectionStringBuilder { DataSource = databasePath }.ToString());

        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = "CREATE TABLE customers (id TEXT NOT NULL PRIMARY KEY); INSERT INTO customers (id) VALUES ('kept');";
        command.ExecuteNonQuery();
    }

    private static float Cosine(float[] a, float[] b)
    {
        double dot = 0, firstNorm = 0, secondNorm = 0;

        for (var index = 0; index < a.Length; index++)
        {
            dot += a[index] * b[index];
            firstNorm += a[index] * a[index];
            secondNorm += b[index] * b[index];
        }

        var scale = Math.Sqrt(firstNorm) * Math.Sqrt(secondNorm);

        return scale == 0 ? 0f : (float)(dot / scale);
    }
}
