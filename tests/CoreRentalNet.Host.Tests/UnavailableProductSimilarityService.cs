using CoreRentalNet.Modules.Catalog.Application.Contracts;

namespace CoreRentalNet.Host.Tests;

/// <summary>A similarity port that fails the way a deployment with no usable vectors does.</summary>
/// <remarks>
/// It stands for both a missing vector file and an embedding server that did not answer, because the adapter
/// turns both into the same exception and the endpoint must answer both the same way. The reason is a
/// parameter so a test can hand it a path and prove the tool does not pass it on.
/// </remarks>
internal sealed class UnavailableProductSimilarityService(string reason = "There is no vector file to search.")
    : IProductSimilarityService
{
    /// <inheritdoc />
    public Task<IReadOnlyList<NearestProduct>> NearestAsync(float[] queryVector,
        CancellationToken cancellationToken)
        => Task.FromException<IReadOnlyList<NearestProduct>>(
            new ProductSimilarityUnavailableException(reason));
}
