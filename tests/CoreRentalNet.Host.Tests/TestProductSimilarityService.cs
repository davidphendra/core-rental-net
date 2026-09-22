using CoreRentalNet.Modules.Catalog.Application.Contracts;

namespace CoreRentalNet.Host.Tests;

/// <summary>
/// A similarity port the API tests answer with: the catalogue's first products, nearest first.
/// </summary>
/// <remarks>
/// It stands in for the vectors so the endpoint's own behaviour - its permission, its filters, its envelope -
/// can be asserted without an embedding server or a vector file.
/// </remarks>
internal sealed class TestProductSimilarityService(IProductCatalogService catalogue) : IProductSimilarityService
{
    /// <summary>How many products it finds, and how many the answer therefore carries.</summary>
    public const int Scored = 3;

    /// <summary>The SKUs it finds, nearest first, in the catalogue's own order.</summary>
    public IReadOnlyList<string> Skus { get; } = [.. catalogue.All.Take(Scored).Select(product => product.Sku)];

    /// <inheritdoc />
    public Task<IReadOnlyList<NearestProduct>> NearestAsync(float[] queryVector,
        CancellationToken cancellationToken)
        => Task.FromResult<IReadOnlyList<NearestProduct>>(
            [.. Skus.Select((sku, index) => new NearestProduct(sku, index * 0.1))]);
}
