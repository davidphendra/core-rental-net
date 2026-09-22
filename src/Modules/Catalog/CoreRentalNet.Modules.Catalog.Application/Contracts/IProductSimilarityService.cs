namespace CoreRentalNet.Modules.Catalog.Application.Contracts;

/// <summary>
/// Finds the products whose stored text is nearest a query vector, so the catalogue can be searched by meaning.
/// </summary>
/// <remarks>
/// <para>
/// <b>A port this module declares and another provides.</b> The vectors are built by the ingestion tool and
/// searched by the discovery module, which this module may not depend on - the dependency runs the other way -
/// so the catalogue states what it needs and the composition root wires the adapter. That is dependency
/// inversion rather than a reach across a module boundary.
/// </para>
/// <para>
/// <b>It takes a vector, not a sentence.</b> Embedding the caller's text is a call to an embedding service,
/// which is the handler's business and not this contract's; what is left here is the search itself. The vector
/// is the one the deployment embeds at, and the freshness check is what keeps it the width the stored vectors
/// were built at.
/// </para>
/// <para>
/// <b>A failure throws.</b> A deployment with no vector file cannot search anything; an empty answer would be
/// indistinguishable from a catalogue with nothing in it.
/// </para>
/// </remarks>
public interface IProductSimilarityService
{
    /// <summary>The products nearest the query vector, nearest first, as the vector store ranked them.</summary>
    /// <exception cref="ProductSimilarityUnavailableException">The vectors cannot be searched right now.</exception>
    Task<IReadOnlyList<NearestProduct>> NearestAsync(float[] queryVector,
        CancellationToken cancellationToken);
}
