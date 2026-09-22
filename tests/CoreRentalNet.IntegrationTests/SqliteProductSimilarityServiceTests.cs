using AwesomeAssertions;
using CoreRentalNet.Modules.Catalog.Application.Contracts;
using CoreRentalNet.Modules.Discovery.Infrastructure.Vectors;
using Xunit;

namespace CoreRentalNet.IntegrationTests;

/// <summary>
/// Searching the tool's own file: the vec0 <c>MATCH</c> query, the blob the query vector is bound as, and the
/// reduction of a product's passages to its nearest one.
/// </summary>
/// <remarks>
/// The ranking itself is sqlite-vec's, so what these tests prove is the wiring: that the stored vectors come
/// back ranked by cosine distance, that several passages become one row per product, and that an unusable file
/// is refused rather than answered as empty.
/// </remarks>
public sealed class SqliteProductSimilarityServiceTests
{
    private const int Width = 384;

    /// <summary>The query vector the stand-in server produces for one sentence: [0, 1, ...].</summary>
    private static float[] Straight() => [.. Enumerable.Range(0, Width).Select(index => (float)index)];

    private static float[] Negated() => [.. Straight().Select(value => -value)];

    [Fact]
    public async Task A_product_is_found_by_its_nearest_passage()
    {
        // The product's first passage points away from the request and its second points straight at it.
        // The nearest one is kept, so the product's distance is ~0; an average would put it half way.
        using var file = new IngestedVectorFile()
            .WithVector("DSK0001", Negated())
            .WithVector("DSK0001", Straight());

        var similarity = new SqliteProductSimilarityService(file.Path);

        var nearest = await similarity.NearestAsync(Straight(), CancellationToken.None);

        nearest.Should().ContainSingle().Which.Sku.Should().Be("DSK0001");
        nearest[0].Distance.Should().BeApproximately(0d, 0.001d);
    }

    [Fact]
    public async Task Every_product_with_a_vector_is_found_once()
    {
        using var file = new IngestedVectorFile()
            .WithVector("DSK0001", Straight())
            .WithVector("DSK0001", Negated())
            .WithVector("CHA0001", Negated());

        var similarity = new SqliteProductSimilarityService(file.Path);

        var nearest = await similarity.NearestAsync(Straight(), CancellationToken.None);

        nearest.Select(product => product.Sku).Should().BeEquivalentTo(["DSK0001", "CHA0001"]);
        nearest.Single(product => product.Sku == "DSK0001").Distance.Should().BeLessThan(
            nearest.Single(product => product.Sku == "CHA0001").Distance);
    }

    [Fact]
    public async Task A_missing_vector_file_is_unavailable_rather_than_empty()
    {
        var missing = Path.Combine(
            Path.GetTempPath(),
            $"core-rental-missing-{Guid.NewGuid():N}",
            "product_embedding.db");

        var similarity = new SqliteProductSimilarityService(missing);

        var act = () => similarity.NearestAsync(Straight(), CancellationToken.None);

        await act.Should().ThrowAsync<ProductSimilarityUnavailableException>();
    }
}
