using Azure;
using Azure.AI.OpenAI;
using CoreRentalNet.BuildingBlocks.Application.Embeddings;

namespace CoreRentalNet.BuildingBlocks.Infrastructure.Embeddings;

/// <summary>Builds the Azure OpenAI embedding adapter a hosted deployment uses.</summary>
public sealed class AzureOpenAiEmbeddingServiceFactory : IEmbeddingServiceFactory
{
    /// <inheritdoc />
    public IEmbeddingService Create(EmbeddingSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        var apiKey = settings.ApiKey;
        ArgumentException.ThrowIfNullOrWhiteSpace(apiKey);

        var client = new AzureOpenAIClient(settings.Endpoint, new AzureKeyCredential(apiKey))
            .GetEmbeddingClient(settings.Model);

        return new EmbeddingClientService(client, settings.Width);
    }
}
