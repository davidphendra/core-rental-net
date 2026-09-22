using CoreRentalNet.Modules.Catalog.Application.Contracts;

namespace CoreRentalNet.Modules.Catalog.UnitTests;

/// <summary>A similarity port a test decides the nearest products of, and can make fail.</summary>
/// <remarks>
/// The search itself lives where the vectors are read, so this stands in for it and lets the handler's own
/// rule - filter, order, and leave out what was never found - be asserted without a vector file.
/// </remarks>
internal sealed class StubProductSimilarityService(params NearestProduct[] nearest) : IProductSimilarityService
{
    /// <summary>Set to make the port fail the way a deployment with no vectors does.</summary>
    public Exception? Failure { get; init; }

    /// <summary>The vector the handler asked with, so a test can assert what reached the port.</summary>
    public float[]? Asked { get; private set; }

    public Task<IReadOnlyList<NearestProduct>> NearestAsync(float[] queryVector,
        CancellationToken cancellationToken)
    {
        Asked = queryVector;

        return Failure is null
            ? Task.FromResult<IReadOnlyList<NearestProduct>>(nearest)
            : Task.FromException<IReadOnlyList<NearestProduct>>(Failure);
    }
}
