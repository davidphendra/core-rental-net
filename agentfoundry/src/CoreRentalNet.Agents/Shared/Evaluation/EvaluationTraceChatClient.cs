using System.Diagnostics;
using System.Runtime.CompilerServices;
using CoreRentalNet.Agents.Shared.ChatClients;
using CoreRentalNet.Agents.Shared.Model;
using CoreRentalNet.Agents.Shared.Telemetry;
using Microsoft.Extensions.AI;

namespace CoreRentalNet.Agents.Shared.Evaluation;

/// <summary>Observes a run without changing the behaviour of the client it wraps.</summary>
/// <remarks>
/// <para>
/// This is the only evaluation code the agent host runs, and it is a decorator: the feature, its executors, its
/// guards and its telemetry client are unaware of it. It records raw evidence — stage name, model answer, tool
/// call, tool answer — and interprets none of it.
/// </para>
/// <para>
/// The tool answers are read from the follow-up model call, because the function-invoking client feeds every
/// result back into the next request. That is what lets tool evidence be captured without a second hook in the
/// tool pipeline.
/// </para>
/// </remarks>
internal sealed class EvaluationTraceChatClient(
    ITelemetryChatClient innerClient,
    IEvaluationTraceSink trace) : DelegatingChatClient(innerClient), ITelemetryChatClient
{
    private readonly HashSet<string> _recordedCallIds = new(StringComparer.Ordinal);

    public AgentRunUsage Total => innerClient.Total;

    public void StartRun(string runId)
    {
        innerClient.StartRun(runId);
        trace.BeginRun(runId, innerClient.Total.Model, innerClient.Total.PromptVersion);
    }

    public void CompleteRun(string runStatus)
    {
        try
        {
            innerClient.CompleteRun(runStatus);
        }
        finally
        {
            trace.CompleteRun(runStatus);
        }
    }

    public override async Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var response = await base.GetResponseAsync(messages, options, cancellationToken);

        RecordCall(
            options,
            messages,
            response.Text,
            ToolCallsOf(response),
            response.Usage);

        return response;
    }

    public override async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var updates = new List<ChatResponseUpdate>();

        await foreach (var update in base.GetStreamingResponseAsync(messages, options, cancellationToken))
        {
            updates.Add(update);
            yield return update;
        }

        RecordStreamingCall(options, messages, updates);
    }

    private void RecordCall(
        ChatOptions? options,
        IEnumerable<ChatMessage> messages,
        string responseText,
        IReadOnlyList<EvaluationToolCall> toolCalls,
        UsageDetails? usage)
    {
        var stage = StageOf(options);
        var attempt = AttemptOf(Activity.Current);

        RecordToolExchanges(stage, attempt, messages);

        trace.RecordStageModelCall(new EvaluationStageModelCall(
            Stage: stage,
            Attempt: attempt,
            ResponseText: responseText,
            ToolCalls: toolCalls,
            InputTokens: (int)(usage?.InputTokenCount ?? 0),
            OutputTokens: (int)(usage?.OutputTokenCount ?? 0),
            Phase: PhaseOf(options)));
    }

    private void RecordStreamingCall(
        ChatOptions? options,
        IEnumerable<ChatMessage> messages,
        IReadOnlyList<ChatResponseUpdate> updates)
    {
        var text = string.Concat(updates.Select(update => update.Text));

        var toolCalls = updates
            .SelectMany(update => update.Contents)
            .OfType<FunctionCallContent>()
            .Select(ToEvaluationToolCall)
            .ToArray();

        var usage = updates
            .SelectMany(update => update.Contents)
            .OfType<UsageContent>()
            .LastOrDefault()?.Details;

        RecordCall(options, messages, text, toolCalls, usage);
    }

    private void RecordToolExchanges(
        string stage,
        int? attempt,
        IEnumerable<ChatMessage> messages)
    {
        var calls = new Dictionary<string, FunctionCallContent>(StringComparer.Ordinal);

        foreach (var call in messages
            .SelectMany(message => message.Contents)
            .OfType<FunctionCallContent>())
        {
            calls[call.CallId ?? string.Empty] = call;
        }

        foreach (var result in messages
            .SelectMany(message => message.Contents)
            .OfType<FunctionResultContent>())
        {
            var callId = result.CallId ?? string.Empty;

            if (!calls.TryGetValue(callId, out var call))
            {
                continue;
            }

            var recordKey = callId.Length > 0
                ? callId
                : $"{call.Name}:{result.Result}";

            if (!_recordedCallIds.Add(recordKey))
            {
                continue;
            }

            trace.RecordToolExchange(new EvaluationToolExchange(
                Stage: stage,
                Attempt: attempt,
                CallId: callId,
                ToolName: call.Name,
                Arguments: call.Arguments is null
                    ? new Dictionary<string, object?>()
                    : new Dictionary<string, object?>(call.Arguments),
                ResultText: result.Result?.ToString() ?? string.Empty));
        }
    }

    private static IReadOnlyList<EvaluationToolCall> ToolCallsOf(ChatResponse response)
        => [.. response.Messages
            .SelectMany(message => message.Contents)
            .OfType<FunctionCallContent>()
            .Select(ToEvaluationToolCall)];

    private static EvaluationToolCall ToEvaluationToolCall(FunctionCallContent call)
        => new(
            call.Name,
            call.Arguments is null
                ? new Dictionary<string, object?>()
                : new Dictionary<string, object?>(call.Arguments));

    private static string StageOf(ChatOptions? options)
        => options?.AdditionalProperties?.TryGetValue(ITelemetryChatClient.AgentName, out var value) is true
           && value is string stage
            ? stage
            : "(unnamed stage)";

    private static int? AttemptOf(Activity? activity)
        => activity?.GetTagItem(WorkspaceTelemetry.RunAttempts) is int attempt
            ? attempt
            : null;

    private static string? PhaseOf(ChatOptions? options)
        => options?.AdditionalProperties?.TryGetValue(ChatCallPhase.Key, out var value) is true
            && value is string phase
            ? phase
            : null;
}
