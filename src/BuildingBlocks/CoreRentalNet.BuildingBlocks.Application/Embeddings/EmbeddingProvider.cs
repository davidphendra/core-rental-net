namespace CoreRentalNet.BuildingBlocks.Application.Embeddings;

/// <summary>The protocol an embedding client speaks.</summary>
public enum EmbeddingProvider
{
    AzureOpenAI,
    OpenAiCompatible,
    Foundry,
}
