using System.ClientModel;
using System.ClientModel.Primitives;
using System.Runtime.CompilerServices;
using Microsoft.Agents.AI;
using OpenAI;
using OpenAI.Responses;

namespace CoreRentalNet.Evaluation.Execution;

/// <summary>Talks to a locally running agent host over the Responses protocol.</summary>
public sealed class AgentHttpClient(
    Uri endpoint,
    string agentName,
    string callerToken) : IAgentClient
{
    private const string PlaceholderKey = "not-needed";

    public async IAsyncEnumerable<string> RunAsync(
        string requestJson,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var options = new OpenAIClientOptions
        {
            Endpoint = endpoint,
            NetworkTimeout = TimeSpan.FromMinutes(10),
        };

        options.AddPolicy(new CallerTokenPolicy(callerToken), PipelinePosition.PerCall);

        var openAiClient = new OpenAIClient(new ApiKeyCredential(PlaceholderKey), options);

#pragma warning disable OPENAI001 // The local Responses surface is the protocol the agent host serves.
        var responsesClient = openAiClient.GetResponsesClient();
#pragma warning restore OPENAI001

        var agent = responsesClient.AsAIAgent(
            model: agentName,
            name: "EvaluationHostedAgent");

        var buffer = new JsonObjectBuffer();

        await foreach (var update in agent.RunStreamingAsync(
            requestJson,
            cancellationToken: cancellationToken))
        {
            foreach (var json in buffer.Append(update.Text))
            {
                yield return json;
            }
        }

        foreach (var json in buffer.Flush())
        {
            yield return json;
        }
    }
}
