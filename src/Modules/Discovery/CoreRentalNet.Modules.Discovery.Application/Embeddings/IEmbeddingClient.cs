namespace CoreRentalNet.Modules.Discovery.Application.Embeddings;

/// <summary>
/// The embedding deployment, as this module needs it.
/// </summary>
/// <remarks>
/// <para>
/// <b>Batched, because the deployment is.</b> One request carries the whole catalogue — 205 texts against a
/// documented limit of 2,048 inputs per call — so there is no per-product call to make and no reason for this
/// contract to have one.
/// </para>
/// <para>
/// <b>A failure throws, and what that means is the caller's decision rather than this contract's.</b> The
/// ingestion tool stops, because a half-embedded catalogue must never reach the file. A suggestion run reports
/// the run unavailable, because the customer gets a retry rather than a wrong shortlist. Folding one of those
/// meanings into the port would force it on the other.
/// </para>
/// </remarks>
public interface IEmbeddingClient
{
    /// <summary>One vector per text, in the order the texts were given.</summary>
    Task<IReadOnlyList<float[]>> EmbedAsync(IReadOnlyList<string> texts, CancellationToken cancellationToken);
}
