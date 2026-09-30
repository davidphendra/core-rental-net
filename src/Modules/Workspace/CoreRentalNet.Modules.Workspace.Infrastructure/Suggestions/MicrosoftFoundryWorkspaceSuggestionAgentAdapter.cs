using System.Runtime.CompilerServices;
using System.Text;
using CoreRentalNet.Modules.Workspace.Application.Suggestions.Agent;
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
public sealed class MicrosoftFoundryWorkspaceSuggestionAgentAdapter : IWorkspaceSuggestionAgentAdapter
{
    private readonly MicrosoftFoundryAgentConnectionSettings _connectionSettings;
    private readonly Func<MicrosoftFoundryAgentConnectionSettings, AIAgent> _agentBuilder;

    public MicrosoftFoundryWorkspaceSuggestionAgentAdapter(MicrosoftFoundryAgentConnectionSettings connectionSettings)
        : this(connectionSettings, MicrosoftFoundryWorkspaceSuggestionAgentAdapterFactory.Build)
    {
    }

    /// <summary>The agentBuilder is built by a delegate so a test can supply one over a fake model client.</summary>
    internal MicrosoftFoundryWorkspaceSuggestionAgentAdapter(
        MicrosoftFoundryAgentConnectionSettings connectionSettings,
        Func<MicrosoftFoundryAgentConnectionSettings, AIAgent> agentBuilder)
    {
        ArgumentNullException.ThrowIfNull(connectionSettings);
        ArgumentNullException.ThrowIfNull(agentBuilder);

        _connectionSettings = connectionSettings;
        _agentBuilder = agentBuilder;
    }

    public async IAsyncEnumerable<WorkspaceSuggestionAgentEvent> StreamSuggestionAsync(
        WorkspaceSuggestionRequestPayload suggestionRequestPayload,
        string callerAccessToken,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var payload = MicrosoftFoundrySuggestionRequestPayload.From(suggestionRequestPayload);
        var agent = TryCreate(out var creationFailure);

        if (agent is null)
        {
            yield return new WorkspaceSuggestionUnavailableAgentEvent(creationFailure!);
            yield break;
        }

        var text = new StringBuilder();

        await foreach (var raised in RelayStream(agent, payload.Json, text, cancellationToken))
        {
            // A failure ends the run here: the stream is not read on after the agent has said it could not answer.
            if (raised is WorkspaceSuggestionUnavailableAgentEvent)
            {
                yield return raised;
                yield break;
            }

            yield return raised;
        }

        foreach (var answered in AnswerOf(text.ToString(), payload.Hash))
        {
            yield return answered;
        }
    }

    /// <summary>What the streamed text amounts to: the progress it announced, then its ending.</summary>
    private static IEnumerable<WorkspaceSuggestionAgentEvent> AnswerOf(string text, string payloadHash)
    {
        var streamedAnswer = MicrosoftFoundrySuggestionAnswerReader.ReadStreamedAnswer(text);

        if (streamedAnswer is null)
        {
            yield return new WorkspaceSuggestionUnavailableAgentEvent(NotTheContract);
            yield break;
        }

        foreach (var progressEvent in ProgressOf(streamedAnswer))
        {
            yield return progressEvent;
        }

        yield return new WorkspaceSuggestionResultReadyAgentEvent(streamedAnswer.Answer, payloadHash);
    }

    /// <summary>What the run announced on its way: the stages it ran, the retries it made, and the setups it
    /// found, in that order and before the ending that follows them.</summary>
    private static IEnumerable<WorkspaceSuggestionAgentEvent> ProgressOf(
        MicrosoftFoundrySuggestionStreamedAnswer streamedAnswer)
    {
        foreach (var agentProcessingStage in streamedAnswer.ProcessingStages)
        {
            yield return new WorkspaceSuggestionStageChangedAgentEvent(agentProcessingStage);
        }

        foreach (var retryAttempt in streamedAnswer.RetryAttempts)
        {
            yield return new WorkspaceSuggestionRetryAgentEvent(
                retryAttempt.NextAttemptNumber, retryAttempt.MaximumAttemptCount);
        }

        foreach (var approvedCandidate in streamedAnswer.Answer.Candidates)
        {
            yield return new WorkspaceSuggestionCandidateApprovedAgentEvent(approvedCandidate);
        }
    }

    /// <summary>What the customer is told when the answer is not the shape the contract promised.</summary>
    private const string NotTheContract = "The agent's answer was not the expected contract.";

    /// <summary>Passes the agent's text on as it arrives, and reads nothing into it.</summary>
    private static async IAsyncEnumerable<WorkspaceSuggestionAgentEvent> RelayStream(
        AIAgent agent,
        string json,
        StringBuilder text,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        AgentSession agentSession = await agent.CreateSessionAsync();
        await using var updates = agent
            .RunStreamingAsync(json, session: agentSession, cancellationToken: cancellationToken)
            .GetAsyncEnumerator(cancellationToken);

        while (true)
        {
            // A yield cannot sit inside a try that catches, so the failure is recorded and reported just after.
            bool moved = false;
            string? failure = null;

            try
            {
                moved = await updates.MoveNextAsync();
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
                yield return new WorkspaceSuggestionUnavailableAgentEvent(failure);
                yield break;
            }

            if (!moved)
            {
                yield break;
            }

            var fragment = updates.Current.Text;

            if (string.IsNullOrEmpty(fragment))
            {
                continue;
            }

            text.Append(fragment);

            yield return new WorkspaceSuggestionNarrativeDeltaAgentEvent(fragment);
        }
    }

    private AIAgent? TryCreate(out string? failure)
    {
        failure = null;

        try
        {
            return _agentBuilder(_connectionSettings);
        }
        catch (Exception exception)
        {
            failure = exception.Message;

            return null;
        }
    }
}
