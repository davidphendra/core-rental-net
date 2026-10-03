using System.Diagnostics;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.AI;
using CoreRentalNet.Agents.Shared.Model;
using CoreRentalNet.Agents.Shared.Telemetry;

namespace CoreRentalNet.Agents.Shared.ChatClients;

/// <summary>Records one span and one line per model call, adds up what the run cost, and owns the run's span.</summary>
/// <remarks>
/// <para>
/// Scoped to the run, because a cost is the run's and not the process's: the stages of one run share this, and a
/// concurrent run has its own. It is the innermost decorator, wrapping the guardrail and the model client, and it
/// names the stage from the call's own options — the agent sets <see cref="ITelemetryChatClient.AgentName"/> — so a
/// call is attributed to the stage that made it.
/// </para>
/// <para>
/// It also holds the run's span, because it is already the run-scoped object the completion nodes read for the
/// run's cost. A second holder would be a second type with the same lifetime for no gain.
/// </para>
/// <para>
/// An interrupted call still counts as a call — the transport attempted it — but its tokens are whatever the
/// response reported, which for a failure is nothing.
/// </para>
/// </remarks>
internal sealed class TelemetryChatClient(
    IChatClient innerClient,
    string modelName,
    string promptVersion,
    ILogger<TelemetryChatClient> logger) : DelegatingChatClient(innerClient), ITelemetryChatClient
{
    private static readonly ActivitySource StageModelCallActivitySource = new("CoreRentalNet.Agents.StageAgent");

    private int _modelCalls;
    private long _inputTokens;
    private long _outputTokens;
    private Activity? _run;

    /// <summary>What the run has cost so far.</summary>
    public AgentRunUsage Total => new(
        _modelCalls,
        (int)Interlocked.Read(ref _inputTokens),
        (int)Interlocked.Read(ref _outputTokens),
        modelName,
        promptVersion);

    /// <summary>Opens the run's span, parented to whatever is current — the platform's server span.</summary>
    public void StartRun(string runId)
    {
        _run = WorkspaceTelemetry.ActivitySource.StartActivity(WorkspaceTelemetry.RunSpanName);
        _run?.SetTag(WorkspaceTelemetry.RunId, runId);
        WorkspaceTelemetry.RunsInFlight.Add(1);
    }

    /// <summary>Closes the run's span, marks its outcome, and counts the run.</summary>
    public void CompleteRun(string runStatus)
    {
        _run?.SetTag(WorkspaceTelemetry.RunStatus, runStatus);
        _run?.SetStatus(
            string.Equals(runStatus, "unavailable", StringComparison.Ordinal)
                ? ActivityStatusCode.Error
                : ActivityStatusCode.Ok);

        WorkspaceTelemetry.RunCount.Add(1, new TagList
        {
            { WorkspaceTelemetry.RunStatus, runStatus },
            { WorkspaceTelemetry.PromptVersion, promptVersion },
        });

        WorkspaceTelemetry.RunsInFlight.Add(-1);
        _run?.Dispose();
        _run = null;
    }

    public override async Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var stageAgentName = StageAgentNameOf(options);
        using var activity = StageModelCallActivitySource.StartActivity("stage-model-call", ActivityKind.Client);
        activity?.SetTag("stage.agent.name", stageAgentName);

        var response = await base.GetResponseAsync(messages, options, cancellationToken);

        Record(activity, stageAgentName, response.Usage);

        return response;
    }

    public override async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var stageAgentName = StageAgentNameOf(options);
        using var activity = StageModelCallActivitySource.StartActivity("stage-model-call", ActivityKind.Client);
        activity?.SetTag("stage.agent.name", stageAgentName);

        var tags = new TagList { { WorkspaceTelemetry.Stage, stageAgentName } };
        var sinceLastToken = Stopwatch.StartNew();
        var sawFirstToken = false;

        UsageDetails? usage = null;

        await foreach (var update in base.GetStreamingResponseAsync(messages, options, cancellationToken))
        {
            if (!string.IsNullOrEmpty(update.Text))
            {
                RecordTokenGap(tags, sinceLastToken, ref sawFirstToken);
            }

            // A stream reports what it cost on one of its updates, not on the call itself, so the figures are
            // summed off the stream the same way the framework's own aggregation sums them.
            foreach (var content in update.Contents)
            {
                if (content is UsageContent usageContent)
                {
                    (usage ??= new UsageDetails()).Add(usageContent.Details);
                }
            }

            yield return update;
        }

        Record(activity, stageAgentName, usage);
    }

    /// <summary>Records the gap to the first token as TTFT and every later gap as per-output-token time.</summary>
    private static void RecordTokenGap(TagList tags, Stopwatch sinceLastToken, ref bool sawFirstToken)
    {
        var instrument = sawFirstToken
            ? WorkspaceTelemetry.ModelTimePerOutputToken
            : WorkspaceTelemetry.ModelTimeToFirstToken;

        instrument.Record(sinceLastToken.Elapsed.TotalMilliseconds, tags);

        sawFirstToken = true;
        sinceLastToken.Restart();
    }

    /// <summary>The stage that made the call, from the name its agent put on the call's options.</summary>
    private static string StageAgentNameOf(ChatOptions? options)
        => options?.AdditionalProperties?.TryGetValue(ITelemetryChatClient.AgentName, out var value) is true
            && value is string stageAgentName
            ? stageAgentName
            : "(unnamed stage)";

    /// <summary>Puts the tokens on the span and in the log, never a prompt and never an answer.</summary>
    private void Record(Activity? activity, string stageAgentName, UsageDetails? usage)
    {
        Interlocked.Increment(ref _modelCalls);

        if (usage is not null)
        {
            Interlocked.Add(ref _inputTokens, usage.InputTokenCount ?? 0);
            Interlocked.Add(ref _outputTokens, usage.OutputTokenCount ?? 0);
        }

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
