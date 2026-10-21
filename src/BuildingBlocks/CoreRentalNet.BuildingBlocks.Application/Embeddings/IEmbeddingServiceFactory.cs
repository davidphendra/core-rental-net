namespace CoreRentalNet.BuildingBlocks.Application.Embeddings;

/// <summary>Builds the one embedding adapter a composition root registers.</summary>
/// <remarks>
/// Construction is a composition concern: the adapter itself should not know whether its client came from
/// Azure OpenAI or from a local OpenAI-compatible server.
/// </remarks>
public interface IEmbeddingServiceFactory
{
    IEmbeddingService Create(EmbeddingSettings settings);
}
