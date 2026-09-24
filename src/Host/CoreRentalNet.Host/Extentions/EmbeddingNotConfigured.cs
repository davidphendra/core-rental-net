using CoreRentalNet.BuildingBlocks.Application.Embeddings;

namespace CoreRentalNet.Host.Extentions;

/// <summary>
/// The deployment that answers when this application has not been told where to embed.
/// </summary>
/// <remarks>
/// <para>
/// <b>Registering nothing would make the container fail to resolve the embedding client</b> and the first
/// search would surface as a server error — a configuration gap dressed up as a fault, which is the one thing a
/// deployment must never be told about its own settings. This answers instead, with a refusal naming the
/// settings that are missing.
/// </para>
/// <para>
/// It throws rather than returning an empty answer, so the search reports itself unavailable and the caller is
/// offered a retry. An empty vector list would be the dangerous alternative: the handler would rank against
/// nothing and answer as if the catalogue held nothing.
/// </para>
/// </remarks>
internal sealed class EmbeddingNotConfigured : IEmbeddingService
{
    private const string Reason =
        "No embedding server is configured, so no search can be embedded. Set VectorEmbedding:Server, "
        + "VectorEmbedding:Model and VectorEmbedding:EmbeddingDatabase, and run CoreRentalNet.CatalogIngestion to fill the "
        + "vector file.";

    /// <inheritdoc />
    public Task<IReadOnlyList<float[]>> EmbedAsync(
        IReadOnlyList<string> texts,
        CancellationToken cancellationToken)
        => Task.FromException<IReadOnlyList<float[]>>(new InvalidOperationException(Reason));
}
