using System.ClientModel;
using Microsoft.Extensions.AI;
using OpenAI;

namespace CoreRentalNet.Evaluation.Evaluation;

/// <summary>Builds the local llama-server chat client used by the judge.</summary>
public static class OpenAiCompatibleJudgeClientFactory
{
    public static IChatClient Create(Uri endpoint, string apiKey, string model)
    {
        ArgumentNullException.ThrowIfNull(endpoint);
        ArgumentException.ThrowIfNullOrWhiteSpace(apiKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(model);

        var options = new OpenAIClientOptions
        {
            Endpoint = endpoint,
        };

        return new OpenAIClient(new ApiKeyCredential(apiKey), options)
            .GetChatClient(model)
            .AsIChatClient();
    }
}
