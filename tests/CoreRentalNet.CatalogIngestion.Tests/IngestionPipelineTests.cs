using AwesomeAssertions;
using CoreRentalNet.CatalogIngestion.Chunking;
using CoreRentalNet.CatalogIngestion.Storage;
using CoreRentalNet.CatalogIngestion.Tests.Chunking;
using CoreRentalNet.CatalogIngestion.Tests.Storage;
using CoreRentalNet.Modules.Catalog.Infrastructure;
using Xunit;

namespace CoreRentalNet.CatalogIngestion.Tests;

/// <summary>
/// The whole chain against a real file: products.json → product → chunk → stored row.
/// </summary>
/// <remarks>
/// The chunker and the server are stand-ins — nothing here says where a boundary should fall or whether a
/// vector is any good — but the catalogue, the renderer and the database are real, so what is proved is that
/// the right number of rows, carrying the right product, reach the file, and that a refusal leaves it alone.
/// </remarks>
public sealed class IngestionPipelineTests
{
    private const int Width = 384;

    private static IngestionPipeline Pipeline(
        string cataloguePath,
        string databasePath,
        IProductChunker chunker,
        int width = Width)
        => new(
            new ProductCatalog(cataloguePath, webRootPath: null),
            chunker,
            new SqliteProductEmbeddingStore(databasePath),
            width,
            TimeProvider.System);

    [Fact]
    public async Task Every_chunk_becomes_a_row_carrying_its_product_details()
    {
        using var catalogue = new TemporaryCatalogFile(CatalogJson.ThreeRows);
        using var database = new TemporaryDatabase();

        var written = await Pipeline(catalogue.Path, database.Path, new StubProductChunker(Width, chunksPerProduct: 2))
            .RunAsync(CancellationToken.None);

        written.Should().Be(6, "three products of two chunks each");

        var rows = StoredRows.Read(database.Path);
        rows.Should().HaveCount(6);
        rows.Select(row => row.SkuNo).Distinct().Should().BeEquivalentTo(["DSK0001", "CHA0001", "MON0001"]);
        rows.Should().OnlyContain(row => row.Name.Contains(row.SkuNo, StringComparison.Ordinal));
    }

    [Fact]
    public async Task Every_product_is_split_from_the_text_its_name_and_description_render_to()
    {
        using var catalogue = new TemporaryCatalogFile(CatalogJson.ThreeRows);
        using var database = new TemporaryDatabase();
        var chunker = new StubProductChunker(Width);

        await Pipeline(catalogue.Path, database.Path, chunker).RunAsync(CancellationToken.None);

        chunker.Texts.Should().HaveCount(3);
        chunker.Texts.Should().OnlyContain(text => text.StartsWith("name: ", StringComparison.Ordinal));
        chunker.Texts.Should().OnlyContain(text => text.Contains("description: ", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Running_again_replaces_rather_than_appends()
    {
        using var catalogue = new TemporaryCatalogFile(CatalogJson.ThreeRows);
        using var database = new TemporaryDatabase();
        var pipeline = Pipeline(catalogue.Path, database.Path, new StubProductChunker(Width, chunksPerProduct: 2));

        await pipeline.RunAsync(CancellationToken.None);
        await pipeline.RunAsync(CancellationToken.None);

        StoredRows.Read(database.Path).Should().HaveCount(6);
    }

    [Fact]
    public async Task A_product_that_yields_no_chunks_is_refused_and_the_stored_rows_survive()
    {
        using var catalogue = new TemporaryCatalogFile(CatalogJson.ThreeRows);
        using var database = new TemporaryDatabase();

        await Pipeline(catalogue.Path, database.Path, new StubProductChunker(Width)).RunAsync(CancellationToken.None);

        // A product with no chunks would be absent from the table, which is a silent omission rather than a
        // smaller result: a query can then never return it and nothing fails.
        var silent = Pipeline(catalogue.Path, database.Path, new StubProductChunker(Width, answerWith: 0));
        var act = async () => await silent.RunAsync(CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*no chunks*");
        StoredRows.Read(database.Path).Should().HaveCount(3);
    }

    [Fact]
    public async Task A_vector_of_the_wrong_width_is_refused_and_the_stored_rows_survive()
    {
        using var catalogue = new TemporaryCatalogFile(CatalogJson.ThreeRows);
        using var database = new TemporaryDatabase();

        await Pipeline(catalogue.Path, database.Path, new StubProductChunker(Width)).RunAsync(CancellationToken.None);

        var narrow = Pipeline(catalogue.Path, database.Path, new StubProductChunker(Width / 2));
        var act = async () => await narrow.RunAsync(CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*was configured*");
        StoredRows.Read(database.Path).Should().HaveCount(3);
    }

    [Fact]
    public void A_catalogue_holding_no_products_is_refused_before_the_pipeline_runs()
    {
        // The loader refuses an empty file, so an empty table is not a state the tool can reach. This asserts
        // the behaviour it depends on: no products is a failure, not an empty result.
        using var catalogue = new TemporaryCatalogFile(CatalogJson.NoRows);

        var act = () => new ProductCatalog(catalogue.Path, webRootPath: null);

        act.Should().Throw<Exception>().WithMessage("*no products*");
    }
}
