using System.ClientModel;
using CoreRentalNet.BuildingBlocks.Application.Embeddings;
using OpenAI;

namespace CoreRentalNet.BuildingBlocks.Infrastructure.Embeddings;

/// <summary>Builds the OpenAI-compatible embedding adapter a local deployment uses.</summary>
public sealed class OpenAiCompatibleEmbeddingServiceFactory : IEmbeddingServiceFactory
{
    private const string LocalKey = "local";

    /// <inheritdoc />
    public IEmbeddingService Create(EmbeddingSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        var apiKey = string.IsNullOrWhiteSpace(settings.ApiKey)
            ? LocalKey
            : settings.ApiKey;

        var client = new OpenAIClient(
                new ApiKeyCredential(apiKey),
                new OpenAIClientOptions { Endpoint = settings.Endpoint })
            .GetEmbeddingClient(settings.Model);

        return new EmbeddingClientService(client, settings.Width);
    }
}
