using System.Runtime.CompilerServices;
using System.Text;
using CoreRentalNet.Modules.Workspace.Application.Suggestions.Agent;
using CoreRentalNet.Modules.Workspace.Application.Suggestions.Agent.Events;
using Microsoft.Agents.AI;

namespace CoreRentalNet.Modules.Workspace.Infrastructure.Suggestions;

/// <summary>The real adapter: it calls the hosted agent and turns its answer into the application's events.</summary>
/// <remarks>
/// <para>
/// <b>Every run gets a session of its own, and none of them is reused.</b> The hosted agent is a singleton shared
/// across customers, so a session carried between runs would carry one customer's request into the next one's
/// answer. A fresh, empty session per run is what makes that impossible rather than unlikely — and nothing is ever
/// written into one, which the remark above used to imply by saying no session was passed at all.
/// </para>
/// <para>
/// A failure the adapter can see becomes <c>Unavailable</c>, which the endpoint reports and the customer
/// retries. A refusal is not a failure and arrives as <c>ResultReady</c> carrying the answer.
/// </para>
/// </remarks>
public sealed class AgentFoundryWorkspaceSuggestionAdapter : IWorkspaceSuggestionAgentAdapter
{
    private readonly AgentFoundryConnectionSetting _connectionSetting;

    public AgentFoundryWorkspaceSuggestionAdapter(AgentFoundryConnectionSetting connectionSetting)
    {
        ArgumentNullException.ThrowIfNull(connectionSetting);

        _connectionSetting = connectionSetting;
    }

    /// <summary>Translates the agent's streamed text into the application's events, as the text arrives.</summary>
    /// <remarks>
    /// <para>
    /// <b>The agent does not emit a <see cref="WorkspaceSuggestionEvent"/>; it emits text.</b> What arrives is a
    /// stream of fragments cut by the transport rather than by the protocol, so a fragment can end in the middle
    /// of a token. This method is the translation boundary - fragments in, the application's own events out - and
    /// the text is both passed on and kept, because its two halves need two different views of the same bytes.
    /// </para>
    /// <para>
    /// <b>Passed on unread, each fragment is a delta</b>, so the panel writes the model's own words as they
    /// arrive rather than after the run has finished. <b>Kept, the fragments become the answer</b>, because the
    /// answer is in none of them: the ending and the setups it ends are read from the whole text once the stream
    /// has stopped, and the reader returns nothing at all until it finds the run's <c>completed</c> event, since
    /// an answer that is still arriving may still be revised. The payload hash is read from that same whole text
    /// because it is over the bytes actually sent, which are known here and nowhere else.
    /// </para>
    /// <para>
    /// <b>Reading the whole text belongs to <see cref="ParseFragment"/> rather than to this method.</b> A <c>yield</c>
    /// cannot sit inside a <c>try</c> that catches, and reading text a model wrote can fail; the tail is a pure
    /// synchronous function of a string, so it is testable without an agent, a transport or a cancellation; and
    /// it keeps the stages, the retries and the setups in one place, in the order the application was promised
    /// them.
    /// </para>
    /// </remarks>
    public async IAsyncEnumerable<WorkspaceSuggestionEvent> StreamAsync(
        WorkspaceSuggestionRequestPayload suggestionRequestPayload,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var requestPayload = AgentFoundrySuggestionRequestPayload.From(suggestionRequestPayload);
        var agentClient = AgentFoundryWorkspaceSuggestionAdapterFactory.Build(_connectionSetting);
        var updateFragments = new StringBuilder();

        await foreach (var raised in RelayUpdateStream(
                                                                agentClient,
                                                                requestPayload.Json,
                                                                updateFragments,
                                                                cancellationToken))
        {
            // A failure ends the run here: the stream is not read on after the agent has said it could not answer.
            if (raised is WorkspaceSuggestionUnavailableEvent)
            {
                yield return raised;
                yield break;
            }

            yield return raised;
        }

        foreach (var answered in ParseFragment(updateFragments.ToString(), requestPayload.Hash))
        {
            yield return answered;
        }
    }

    /// <summary>What the streamed text amounts to: the progress it announced, then its ending.</summary>
    private static IEnumerable<WorkspaceSuggestionEvent> ParseFragment(string text, string payloadHash)
    {
        var streamedAnswer = WorkspaceSuggestionFragmentReader.Parse(text);
        if (streamedAnswer is null)
        {
            yield return new WorkspaceSuggestionUnavailableEvent(NotTheContract);
            yield break;
        }

        foreach (var progressEvent in ProgressOf(streamedAnswer))
        {
            yield return progressEvent;
        }

        yield return new WorkspaceSuggestionResultReadyEvent(streamedAnswer.Answer, payloadHash);
    }

    /// <summary>What the run announced on its way: the stages it ran, the retries it made, and the setups it
    /// found, in that order and before the ending that follows them.</summary>
    private static IEnumerable<WorkspaceSuggestionEvent> ProgressOf(
        WorkspaceSuggestionAnswerDetail answerDetail)
    {
        foreach (var processingStage in answerDetail.ProcessingStages)
        {
            yield return new WorkspaceSuggestionChangedEvent(processingStage);
        }

        foreach (var retryAttempt in answerDetail.RetryAttempts)
        {
            yield return new WorkspaceSuggestionRetryEvent(
                retryAttempt.NextAttemptNumber, retryAttempt.MaximumAttemptCount);
        }

        foreach (var approvedCandidate in answerDetail.Answer.Candidates)
        {
            yield return new WorkspaceSuggestionCandidateApprovedEvent(approvedCandidate);
        }
    }

    /// <summary>What the customer is told when the answer is not the shape the contract promised.</summary>
    private const string NotTheContract = "The agent's answer was not the expected contract.";

    /// <summary>Passes the agent's agentUpdateBuilder on as it arrives, and reads nothing into it.</summary>
    private static async IAsyncEnumerable<WorkspaceSuggestionEvent> RelayUpdateStream(
        AIAgent agent,
        string payloadJson,
        StringBuilder agentUpdateBuilder,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        AgentSession agentSession = await agent.CreateSessionAsync(cancellationToken);
        await using var suggestionEnumerator = agent
            .RunStreamingAsync(payloadJson, session: agentSession, cancellationToken: cancellationToken)
            .GetAsyncEnumerator(cancellationToken);

        while (true)
        {
            // A yield cannot sit inside a try that catches, so the failure is recorded and reported just after.
            bool moved = false;
            string? failure = null;

            try
            {
                moved = await suggestionEnumerator.MoveNextAsync();
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                // The customer stopped the run, or their browser went away. That is an <b>ending</b>, not an
                // unavailable run, so it is rethrown as itself rather than dressed up as one.
                throw;
            }
            catch (Exception exception)
            {
                failure = exception.Message;
            }

            if (failure is not null)
            {
                yield return new WorkspaceSuggestionUnavailableEvent(failure);
                yield break;
            }

            if (!moved)
            {
                yield break;
            }

            var fragment = suggestionEnumerator.Current.Text;

            if (string.IsNullOrEmpty(fragment))
            {
                continue;
            }

            agentUpdateBuilder.Append(fragment);

            yield return new WorkspaceSuggestionNarrativeDeltaEvent(fragment);
        }
    }
}
