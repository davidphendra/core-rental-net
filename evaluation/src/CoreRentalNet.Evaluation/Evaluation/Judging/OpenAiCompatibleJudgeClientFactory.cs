using System.ClientModel;
using Microsoft.Extensions.AI;
using OpenAI;

using CoreRentalNet.Evaluation.Evaluation.Pipeline;
using CoreRentalNet.Evaluation.Evaluation.Checks;
using CoreRentalNet.Evaluation.Evaluation.Timing;
using CoreRentalNet.Evaluation.Evaluation.Stochastic;
namespace CoreRentalNet.Evaluation.Evaluation.Judging;

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
