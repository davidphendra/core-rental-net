namespace CoreRentalNet.CatalogIngestion.Embeddings;

/// <summary>The embedding server, as this tool needs it.</summary>
/// <remarks>
/// <para>
/// <b>Batched, because the server is.</b> One request carries every text the caller has, against a server
/// that accepts many inputs per call, so there is no per-text call to make and no reason for this contract
/// to have one.
/// </para>
/// <para>
/// <b>A failure throws, and what that means is the caller's decision.</b> The ingestion stops, because a
/// half-embedded catalogue must never replace a good table. Folding that meaning into the port would decide
/// it here, where the reason for the failure is not yet known.
/// </para>
/// </remarks>
internal interface IEmbeddingClient
{
    /// <summary>One vector per text, in the order the texts were given.</summary>
    Task<IReadOnlyList<float[]>> EmbedAsync(IReadOnlyList<string> texts, CancellationToken cancellationToken);
}
