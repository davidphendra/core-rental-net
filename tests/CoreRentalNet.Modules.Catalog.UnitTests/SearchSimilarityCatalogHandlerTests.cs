using AwesomeAssertions;
using CoreRentalNet.Modules.Catalog.Application.Contracts;
using CoreRentalNet.Modules.Catalog.Application.Queries.SearchSimilarityCatalog;
using CoreRentalNet.Modules.Catalog.Infrastructure;
using Xunit;

namespace CoreRentalNet.Modules.Catalog.UnitTests;

/// <summary>
/// The similarity search's rule: the filters narrow, the distance orders, and a product that was never found is
/// left out.
/// </summary>
/// <remarks>
/// The vector <c>MATCH</c> itself is asserted where it lives, in the adapter's own tests. What is asserted here
/// is what this handler does with the nearest products it is handed.
/// </remarks>
public sealed class SearchSimilarityCatalogHandlerTests
{
    private static readonly float[] QueryVector = [0.1f, 0.2f];

    private static SearchSimilarityCatalogHandler Handler(
        TemporaryCatalogFile file,
        out StubEmbeddingService embeddings,
        out StubProductSimilarityService similarityService,
        params NearestProduct[] nearest)
    {
        embeddings = new StubEmbeddingService(QueryVector);
        similarityService = new StubProductSimilarityService(nearest);

        return new SearchSimilarityCatalogHandler(
            new ProductCatalogService(file.Path, null),
            embeddings,
            similarityService);
    }

    [Fact]
    public async Task The_nearest_product_comes_first()
    {
        // The distance is sqlite-vec's cosine distance, so the smallest is nearest.
        using var file = new TemporaryCatalogFile(SampleCatalog.SevenRows);
        var handler = Handler(
            file,
            out _,
            out _,
            new NearestProduct("CHA0001", 0.9),
            new NearestProduct("DSK0001", 0.1),
            new NearestProduct("MON0001", 0.5));

        var products = await handler.HandleAsync(
            new SearchSimilarityCatalogQuery(null, null, "something to work at"),
            CancellationToken.None);

        products.Select(product => product.Sku).Should().Equal("DSK0001", "MON0001", "CHA0001");
    }

    [Fact]
    public async Task The_filters_narrow_before_the_distance_orders()
    {
        // The monitor is nearest overall and the chair is farther, but the subcategory filter still decides,
        // so only the monitor is eligible and therefore only the monitor is returned.
        using var file = new TemporaryCatalogFile(SampleCatalog.SevenRows);
        var handler = Handler(
            file,
            out _,
            out _,
            new NearestProduct("MON0001", 0.2),
            new NearestProduct("CHA0001", 0.9),
            new NearestProduct("DSK0001", 0.8));

        var products = await handler.HandleAsync(
            new SearchSimilarityCatalogQuery(CatalogCategory.Accessory, CatalogSubCategory.Monitor, "anything"),
            CancellationToken.None);

        products.Should().ContainSingle().Which.Sku.Should().Be("MON0001");
    }

    [Fact]
    public async Task Equal_distances_are_ordered_by_sku()
    {
        // Two products at the same distance still have to come back in a reproducible order.
        using var file = new TemporaryCatalogFile(SampleCatalog.SevenRows);
        var handler = Handler(
            file,
            out _,
            out _,
            new NearestProduct("DSK0001", 0.5),
            new NearestProduct("CHA0001", 0.5));

        var products = await handler.HandleAsync(
            new SearchSimilarityCatalogQuery(null, null, "anything"),
            CancellationToken.None);

        products.Select(product => product.Sku).Should().Equal("CHA0001", "DSK0001");
    }

    [Fact]
    public async Task A_product_that_was_not_found_is_left_out()
    {
        // Seven products are eligible and only one was found: the answer is the one that can be ranked, not
        // the seven with six of them at zero - which would look like a complete answer.
        using var file = new TemporaryCatalogFile(SampleCatalog.SevenRows);
        var handler = Handler(file, out _, out _, new NearestProduct("PLT0001", 0.7));

        var products = await handler.HandleAsync(
            new SearchSimilarityCatalogQuery(null, null, "anything"),
            CancellationToken.None);

        products.Should().ContainSingle().Which.Sku.Should().Be("PLT0001");
    }

    [Fact]
    public async Task The_sentence_reaches_the_embedding_service_verbatim()
    {
        using var file = new TemporaryCatalogFile(SampleCatalog.SevenRows);
        var handler = Handler(file, out var embeddings, out _);

        await handler.HandleAsync(
            new SearchSimilarityCatalogQuery(null, null, "  two screens, badly lit  "),
            CancellationToken.None);

        embeddings.Asked.Should().Equal("  two screens, badly lit  ");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task A_blank_sentence_is_refused(string sentence)
    {
        using var file = new TemporaryCatalogFile(SampleCatalog.SevenRows);
        var handler = Handler(file, out _, out _);

        var act = () => handler.HandleAsync(
            new SearchSimilarityCatalogQuery(null, null, sentence),
            CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task A_search_that_cannot_be_made_is_not_answered_as_empty()
    {
        using var file = new TemporaryCatalogFile(SampleCatalog.SevenRows);
        var handler = new SearchSimilarityCatalogHandler(
            new ProductCatalogService(file.Path, null),
            new StubEmbeddingService(QueryVector),
            new StubProductSimilarityService { Failure = new ProductSimilarityUnavailableException("no vectors") });

        var act = () => handler.HandleAsync(
            new SearchSimilarityCatalogQuery(null, null, "anything"),
            CancellationToken.None);

        await act.Should().ThrowAsync<ProductSimilarityUnavailableException>();
    }
}
