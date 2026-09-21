namespace CoreRentalNet.Modules.Discovery.Application.Indexing;

/// <summary>
/// Answers whether the index can be searched by this deployment.
/// </summary>
/// <remarks>
/// <b>The four things that matter are the four the index records</b> — the catalogue it was built from, the
/// model that embedded it, the width of its vectors, and the composition of the text they came from. Each of
/// them silently invalidates every vector when it changes, and none of them fails loudly on its own: a search
/// over vectors built from a different model, or a different text, still returns fourteen products and simply
/// ranks them wrongly. This is the check that turns that into a fact somebody can act on.
/// </remarks>
public interface ICatalogIndexFreshness
{
    /// <summary>Whether the stored index matches what this deployment would build.</summary>
    /// <param name="catalogueHash">The hash of the catalogue as it is now.</param>
    /// <param name="modelId">The model the deployment is configured to embed with.</param>
    /// <param name="width">The width the deployment is configured to embed at.</param>
    /// <param name="cancellationToken">The caller's token.</param>
    Task<CatalogIndexVerdict> CheckAsync(
        string catalogueHash,
        string modelId,
        int width,
        CancellationToken cancellationToken);
}
