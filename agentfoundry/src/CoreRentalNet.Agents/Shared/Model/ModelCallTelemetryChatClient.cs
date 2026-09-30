using System.Diagnostics;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace CoreRentalNet.Agents.Shared.Model;

/// <summary>Records one span and one line for every model call a stage makes.</summary>
/// <remarks>
/// A run is several model calls across several stages, and the workflow's own events say which stage ran but not
/// what it cost. This is where a stage's tokens are observed, so a run's cost can be attributed to the stage that
/// spent it rather than to the run as a whole.
/// </remarks>
internal sealed class ModelCallTelemetryChatClient(
    IChatClient innerClient,
    string stageAgentName,
    AgentRunUsageAccumulator runUsage,
    ILogger<ModelCallTelemetryChatClient> logger) : DelegatingChatClient(innerClient)
{
    private static readonly ActivitySource StageModelCallActivitySource = new("CoreRentalNet.Agents.StageAgent");

    public override async Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        using var activity = StageModelCallActivitySource.StartActivity("stage-model-call", ActivityKind.Client);
        activity?.SetTag("stage.agent.name", stageAgentName);

        var response = await base.GetResponseAsync(messages, options, cancellationToken);

        Record(activity, response.Usage);

        return response;
    }

    public override async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        using var activity = StageModelCallActivitySource.StartActivity("stage-model-call", ActivityKind.Client);
        activity?.SetTag("stage.agent.name", stageAgentName);

        await foreach (var update in base.GetStreamingResponseAsync(messages, options, cancellationToken))
        {
            yield return update;
        }
    }

    /// <summary>Puts the tokens on the span and in the log, never a prompt and never an answer.</summary>
    private void Record(Activity? activity, UsageDetails? usage)
    {
        runUsage.Record(usage);

        var inputTokens = usage?.InputTokenCount ?? 0;
        var outputTokens = usage?.OutputTokenCount ?? 0;

        activity?.SetTag("gen_ai.usage.input_tokens", inputTokens);
        activity?.SetTag("gen_ai.usage.output_tokens", outputTokens);

        logger.LogInformation(
            "Stage agent {StageAgentName} made a model call: {InputTokens} input tokens, {OutputTokens} output tokens.",
            stageAgentName,
            inputTokens,
            outputTokens);
    }
}
