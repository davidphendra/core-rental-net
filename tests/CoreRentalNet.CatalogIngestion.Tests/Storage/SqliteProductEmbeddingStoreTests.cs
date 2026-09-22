using System.Globalization;
using AwesomeAssertions;
using CoreRentalNet.BuildingBlocks.Application.Embeddings;
using CoreRentalNet.CatalogIngestion.Storage;
using Microsoft.Data.Sqlite;
using Xunit;

namespace CoreRentalNet.CatalogIngestion.Tests.Storage;

/// <summary>
/// The vector table and its recipe, in their own SQLite file, written and read back the way a consumer would.
/// </summary>
/// <remarks>
/// Every test reads the rows back with SQL rather than asserting what the tool was asked to write, because
/// what a future searcher depends on is the file, not the call.
/// </remarks>
public sealed class SqliteProductEmbeddingStoreTests
{
    private static readonly DateTimeOffset Stamp = new(2026, 9, 21, 12, 0, 0, TimeSpan.Zero);

    private static readonly EmbeddingRecipe Recipe = new(
        "all-MiniLM-L6-v2-embedding",
        384,
        "name+description+metadata/1",
        "0f1e2d3c4b5a6978");

    private static ProductEmbeddingRow Row(string sku, params float[] leading)
        => new(sku, $"{sku} name", $"{sku} description", Padded(leading));

    /// <summary>A full-width vector whose leading values are the ones a test cares about.</summary>
    private static float[] Padded(float[] leading)
    {
        var vector = new float[384];
        leading.CopyTo(vector, 0);

        return vector;
    }

    private static IProductEmbeddingStore Store(TemporaryDatabase database)
        => new SqliteProductEmbeddingStore(database.Path);

    [Fact]
    public void The_first_write_creates_the_table_and_holds_every_row()
    {
        using var database = new TemporaryDatabase();

        Store(database).ReplaceAll([Row("A", 1f, 0f), Row("A", 0f, 1f), Row("B", 1f, 1f)], Recipe, Stamp);

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

        store.ReplaceAll([Row("A", 1f, 0f), Row("B", 1f, 0f)], Recipe, Stamp);
        store.ReplaceAll([Row("C", 1f, 0f)], Recipe, Stamp);

        StoredRows.Read(database.Path).Should().ContainSingle().Which.SkuNo.Should().Be("C");
    }

    [Fact]
    public void Every_row_gets_its_own_id_and_the_run_s_timestamp()
    {
        using var database = new TemporaryDatabase();

        Store(database).ReplaceAll([Row("A", 1f, 0f), Row("A", 0f, 1f)], Recipe, Stamp);

        var rows = StoredRows.Read(database.Path);
        var expected = Stamp.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture);

        rows.Select(row => row.Id).Should().OnlyHaveUniqueItems();
        rows.Should().OnlyContain(row => row.CreatedAt == expected);
    }

    [Fact]
    public void The_stored_vector_decodes_to_the_floats_that_were_written()
    {
        using var database = new TemporaryDatabase();

        Store(database).ReplaceAll([Row("A", 0.5f, -1.25f, 3f)], Recipe, Stamp);

        StoredRows.Read(database.Path).Single().Embedding.Take(3).Should().Equal(0.5f, -1.25f, 3f);
    }

    [Fact]
    public void The_nearest_row_to_a_query_is_the_one_sqlite_vec_ranks_first()
    {
        // The search the tool does not have: a consumer's MATCH query ranks the stored rows. It is asserted
        // here because it is the property the whole table exists for.
        using var database = new TemporaryDatabase();

        Store(database).ReplaceAll(
            [Row("ALIGNED", 1f, 0f), Row("SIDEWAYS", 0f, 1f), Row("DIAGONAL", 0.7f, 0.7f)],
            Recipe,
            Stamp);

        StoredRows.Nearest(database.Path, Padded([1f, 0f])).Should().Equal("ALIGNED", "DIAGONAL", "SIDEWAYS");
    }

    [Fact]
    public void The_recipe_records_what_built_the_vectors()
    {
        // The application refuses to search without this, so a run that wrote vectors and no recipe would
        // leave a file that looks built and cannot be used.
        using var database = new TemporaryDatabase();

        Store(database).ReplaceAll([Row("A", 1f, 0f)], Recipe, Stamp);

        var recipe = StoredRecipes.Read(database.Path);

        recipe.Should().NotBeNull();
        recipe!.ModelId.Should().Be(Recipe.ModelId);
        recipe.Width.Should().Be(Recipe.Width);
        recipe.Composition.Should().Be(Recipe.Composition);
        recipe.CatalogueHash.Should().Be(Recipe.CatalogueHash);
        recipe.CreatedAt.Should().Be(Stamp.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture));
    }

    [Fact]
    public void A_second_write_replaces_the_recipe_too()
    {
        // Two recipe rows would be a file that answers "what built this?" twice, and the reader takes the
        // first: the answer would then be the run before last.
        using var database = new TemporaryDatabase();
        var store = Store(database);

        store.ReplaceAll([Row("A", 1f, 0f)], Recipe, Stamp);
        store.ReplaceAll([Row("A", 1f, 0f)], Recipe with { ModelId = "a-newer-model" }, Stamp);

        var recipe = StoredRecipes.Read(database.Path);

        recipe.Should().NotBeNull();
        recipe!.ModelId.Should().Be("a-newer-model");
    }

    [Fact]
    public void A_database_holding_a_foreign_table_is_refused_rather_than_emptied()
    {
        // A Database:Path pointed at an unrelated SQLite file would otherwise have its rows deleted, silently.
        using var database = new TemporaryDatabase();
        CreateForeignDatabase(database.Path);

        var act = () => Store(database).ReplaceAll([Row("A", 1f, 0f)], Recipe, Stamp);

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
}
