using System.ClientModel;
using System.ClientModel.Primitives;
using Microsoft.Extensions.AI;
using OpenAI;

namespace CoreRentalNet.Agents.Shared.Model;

/// <summary>Builds a chat client for an OpenAI-compatible local server.</summary>
internal sealed class OpenAiCompatibleChatClientFactory(
    Uri endpoint,
    string apiKey,
    string model,
    ModelTransportRetryOptions transport) : IChatClientFactory
{
    /// <summary>The local server's endpoint, which a local run must set.</summary>
    public const string EndpointKey = "OPENAI_COMPATIBLE_ENDPOINT";

    /// <summary>The local server's key. A llama server ignores it, but the client requires one.</summary>
    public const string ApiKeyKey = "OPENAI_COMPATIBLE_API_KEY";

    /// <inheritdoc />
    public IChatClient Create()
    {
        var options = new OpenAIClientOptions
        {
            Endpoint = endpoint,
            NetworkTimeout = transport.NetworkTimeout,
            RetryPolicy = new ClientRetryPolicy(transport.MaximumRetryAttempts),
        };

        return new OpenAIClient(new ApiKeyCredential(apiKey), options)
            .GetChatClient(model)
            .AsIChatClient();
    }
}
