using System.Runtime.CompilerServices;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using WorkspaceSuggestions.Contracts;
using WorkspaceSuggestions.Usage;

namespace WorkspaceSuggestions.Workflows;

/// <summary>Publishes the answer with the run's cost attached to it.</summary>
/// <remarks>
/// <para>
/// It opens the counting scope for one run, lets the workflow inside do its work, then appends a
/// <see cref="RunUsageReport"/> as its own object. <b>Appended rather than merged</b>: the model's JSON is
/// streamed verbatim and is already gone by the time this sees the finished response, so a figure added here
/// cannot be injected into an object that has already been sent.
/// </para>
/// <para>
/// On the streamed path the report is the last thing the customer's application receives, which is what lets
/// it be read from the same accumulated text as everything else. On the unstreamed path it is appended to the
/// response's own text, so both paths answer the same shape and a test can assert one rule.
/// </para>
/// </remarks>
internal sealed class UsageReportingAgent(AIAgent inner, string model, string promptVersion) : DelegatingAIAgent(inner)
{
    protected override async Task<AgentResponse> RunCoreAsync(
        IEnumerable<ChatMessage> messages,
        AgentSession? session = null,
        AgentRunOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        using var scope = RunUsageScope.Begin();

        var response = await base.RunCoreAsync(messages, session, options, cancellationToken).ConfigureAwait(false);

        return Append(response, Report(scope.Recorder));
    }

    protected override async IAsyncEnumerable<AgentResponseUpdate> RunCoreStreamingAsync(
        IEnumerable<ChatMessage> messages,
        AgentSession? session = null,
        AgentRunOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        using var scope = RunUsageScope.Begin();

        await foreach (var update in base
            .RunCoreStreamingAsync(messages, session, options, cancellationToken)
            .ConfigureAwait(false))
        {
            yield return update;
        }

        // The counts are only complete once the workflow has finished, which is why this is last.
        yield return new AgentResponseUpdate(ChatRole.Assistant, Report(scope.Recorder));
    }

    private string Report(UsageRecorder recorder)
        => ContractJson.Serialize(new RunUsageReport(recorder.Snapshot(model, promptVersion)));

    private static AgentResponse Append(AgentResponse response, string report)
        => new([.. response.Messages, new ChatMessage(ChatRole.Assistant, report)]);
}
