namespace CoreRentalNet.BuildingBlocks.Application.Embeddings;

/// <summary>
/// An embedding service, as anything in this solution needs it.
/// </summary>
/// <remarks>
/// <para>
/// <b>Batched, because the servers are.</b> One request carries a whole catalogue — 205 texts against a
/// documented limit of 2,048 inputs per call — so there is no per-text call to make and no reason for this
/// contract to have one.
/// </para>
/// <para>
/// <b>A failure throws, and what that means is the caller's decision rather than this contract's.</b> The
/// ingestion tool stops, because a half-embedded catalogue must never reach the file. A suggestion run reports
/// itself unavailable, because the customer gets a retry rather than a wrong shortlist. Folding one of those
/// meanings into the port would force it on the other.
/// </para>
/// <para>
/// <b>It is in the shared kernel because two composition roots embed with it</b>: the tool that builds the
/// vector table, and the application that embeds every customer's sentence. The two used to declare a port
/// each, of the same shape, which is two definitions of one contract — and the second one is the one that
/// drifts.
/// </para>
/// </remarks>
public interface IEmbeddingRepository
{
    /// <summary>One vector per text, in the order the texts were given.</summary>
    Task<IReadOnlyList<float[]>> EmbedAsync(IReadOnlyList<string> texts, CancellationToken cancellationToken);
}
