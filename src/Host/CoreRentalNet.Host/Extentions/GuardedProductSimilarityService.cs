using CoreRentalNet.Modules.Catalog.Application.Contracts;

namespace CoreRentalNet.Host.Extentions;

/// <summary>
/// Refuses a similarity search when the stored vectors are not the ones this deployment would build.
/// </summary>
/// <remarks>
/// <para>
/// <b>The refusal is a decorator rather than a check inside the endpoint</b>, so the action has no failure
/// branch and one answer - the 503 <see cref="ProductSimilarityUnavailableException"/> maps to - covers both a
/// stale index and an embedding outage.
/// </para>
/// <para>
/// Ranking on a stale index would return plausible products scored against vectors from another model or
/// another catalogue: the exact failure the freshness check exists to make loud. When the index is usable this
/// forwards to the adapter untouched.
/// </para>
/// </remarks>
internal sealed class GuardedProductSimilarityService(
    IProductSimilarityService similarityService,
    CatalogIndexAvailability availability) : IProductSimilarityService
{
    /// <inheritdoc />
    public Task<IReadOnlyList<NearestProduct>> NearestAsync(float[] queryVector,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(queryVector);

        return availability.IsUsable
            ? similarityService.NearestAsync(queryVector, cancellationToken)
            : Task.FromException<IReadOnlyList<NearestProduct>>(
                new ProductSimilarityUnavailableException(
                    $"The stored catalogue vectors cannot be searched. {availability.Reason}"));
    }
}
