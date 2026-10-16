using CoreRentalNet.Modules.Catalog.Application.Contracts;

namespace CoreRentalNet.Host.Tests;

/// <summary>
/// A similarity port the API tests answer with: the catalogue's first products, nearest first.
/// </summary>
/// <remarks>
/// It stands in for the vectors so the endpoint's own behaviour - its permission, its filters, its envelope -
/// can be asserted without an embedding server or a vector file.
/// </remarks>
internal sealed class TestProductSimilarityService : IProductSimilarityService
{
    /// <summary>How many products it finds, and how many the answer therefore carries.</summary>
    public const int Scored = 3;

    public TestProductSimilarityService(IProductCatalogService catalogue)
    {
        ArgumentNullException.ThrowIfNull(catalogue);

        // The catalogue no longer exposes every product; a scoped page of it is enough for a stand-in.
        Skus =
        [
            .. catalogue
                .SearchPageAsync(new CatalogScope(CatalogCategory.Desk, null), null, 0, Scored, CancellationToken.None)
                .GetAwaiter()
                .GetResult()
                .Items
                .Select(product => product.Sku),
        ];
    }

    /// <summary>The SKUs it finds, nearest first, in the catalogue's own order.</summary>
    public IReadOnlyList<string> Skus { get; }

    /// <inheritdoc />
    public Task<IReadOnlyList<NearestProduct>> NearestAsync(float[] queryVector,
        CancellationToken cancellationToken)
        => Task.FromResult<IReadOnlyList<NearestProduct>>(
            [.. Skus.Select((sku, index) => new NearestProduct(sku, index * 0.1))]);
}
