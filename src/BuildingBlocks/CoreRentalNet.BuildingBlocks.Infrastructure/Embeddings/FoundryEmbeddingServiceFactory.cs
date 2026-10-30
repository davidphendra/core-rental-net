using Azure.AI.OpenAI;
using Azure.AI.Projects;
using Azure.Identity;
using CoreRentalNet.BuildingBlocks.Application.Embeddings;

namespace CoreRentalNet.BuildingBlocks.Infrastructure.Embeddings;

/// <summary>Builds the embedding adapter for a Microsoft Foundry project.</summary>
/// <remarks>
/// <para>
/// <b>A Foundry project is not an OpenAI-compatible server.</b> Its endpoint addresses a project, and the
/// project's Azure OpenAI connection is resolved before an embedding client can be built. The credential is
/// the signed-in Azure CLI identity; there is no API key in this path.
/// </para>
/// <para>
/// <b>Only the final protocol adapter is shared with the other providers.</b> The connection, endpoint and
/// authentication rules here are Foundry's own, so a change to the OpenAI-compatible path cannot alter them.
/// </para>
/// </remarks>
public sealed class FoundryEmbeddingServiceFactory : IEmbeddingServiceFactory
{
    /// <inheritdoc />
    public IEmbeddingService Create(EmbeddingSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        var credential = new AzureCliCredential();
        var projectClient = new AIProjectClient(settings.Endpoint, credential);
        var connection = projectClient.GetConnection(typeof(AzureOpenAIClient).FullName!);

        if (!connection.TryGetLocatorAsUri(out var locator) || locator is null)
        {
            throw new InvalidOperationException(
                "The Foundry project has no Azure OpenAI connection that can be used for embeddings.");
        }

        var endpoint = EmbeddingEndpoint(settings.Endpoint);

        var client = new AzureOpenAIClient(endpoint, credential)
            .GetEmbeddingClient(settings.Model);

        return new EmbeddingClientService(client, settings.Width);
    }

    /// <summary>The account's Azure OpenAI endpoint, derived from the Foundry project endpoint.</summary>
    /// <remarks>
    /// A Foundry project endpoint is <c>https://&lt;account&gt;.services.ai.azure.com/api/projects/&lt;project&gt;</c>.
    /// The project's OpenAI connection locator points back at the project host, but the Azure OpenAI embedding
    /// route is served by the account's <c>cognitiveservices.azure.com</c> host.
    /// </remarks>
    private static Uri EmbeddingEndpoint(Uri projectEndpoint)
    {
        const string projectSuffix = ".services.ai.azure.com";

        if (!projectEndpoint.Host.EndsWith(projectSuffix, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"The Foundry project endpoint '{projectEndpoint}' does not use the expected host suffix '{projectSuffix}'.");
        }

        var accountHost = projectEndpoint.Host[..^projectSuffix.Length] + ".cognitiveservices.azure.com";

        return new Uri($"https://{accountHost}/");
    }
}
