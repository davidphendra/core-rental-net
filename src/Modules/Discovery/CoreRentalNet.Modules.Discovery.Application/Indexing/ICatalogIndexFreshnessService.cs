namespace CoreRentalNet.Modules.Discovery.Application.Indexing;

/// <summary>
/// Answers whether the index can be searched by this deployment.
/// </summary>
/// <remarks>
/// <para>
/// <b>The four things that matter are the four the index records</b> — the catalogue it was built from, the
/// model that embedded it, the width of its vectors, and the composition of the text they came from. Each of
/// them silently invalidates every vector when it changes, and none of them fails loudly on its own: a search
/// over vectors built from a different model, or a different text, still returns fourteen products and simply
/// ranks them wrongly. This is the check that turns that into a fact somebody can act on.
/// </para>
/// <para>
/// <b>The composition is given to this port rather than found by it.</b> The vectors are built by the ingestion
/// tool, so the text contract lives with that tool and this module holds only the comparison — a renderer here
/// would be a second definition of a product's text, which is exactly what the two used to have.
/// </para>
/// </remarks>
public interface ICatalogIndexFreshnessService
{
    /// <summary>Whether the stored vectors match what this deployment would build.</summary>
    /// <param name="catalogueHash">The hash of the catalogue as it is now.</param>
    /// <param name="modelId">The model the deployment is configured to embed with.</param>
    /// <param name="width">The width the deployment is configured to embed at.</param>
    /// <param name="composition">What the ingestion tool renders a product's text from.</param>
    CatalogIndexVerdict Check(string catalogueHash, string modelId, int width, string composition);
}
