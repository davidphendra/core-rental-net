using CoreRentalNet.BuildingBlocks.Application.Embeddings;

namespace CoreRentalNet.BuildingBlocks.Infrastructure.Embeddings;

/// <summary>Selects the embedding factory a deployment's configuration asks for.</summary>
/// <remarks>
/// The switch is deliberately at the composition boundary. A new provider adds a factory and one case here;
/// the adapter, the discovery registration and the ingestion pipeline stay unchanged.
/// </remarks>
public static class EmbeddingServiceFactorySelector
{
    /// <summary>The factory for one provider.</summary>
    public static IEmbeddingServiceFactory Select(EmbeddingProvider provider)
        => provider switch
        {
            EmbeddingProvider.AzureOpenAI => new AzureOpenAiEmbeddingServiceFactory(),
            EmbeddingProvider.OpenAiCompatible => new OpenAiCompatibleEmbeddingServiceFactory(),
            EmbeddingProvider.Foundry => new FoundryEmbeddingServiceFactory(),
            _ => throw new ArgumentOutOfRangeException(
                nameof(provider),
                provider,
                "Unknown embedding provider."),
        };
}
