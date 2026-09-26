using System.Runtime.CompilerServices;
using System.Text;
using CoreRentalNet.Modules.Workspace.Application.Suggestions.Agent;
using Microsoft.Agents.AI;

namespace CoreRentalNet.Modules.Workspace.Infrastructure.Suggestions;

/// <summary>The real adapter: it calls the hosted agent and turns its answer into the application's events.</summary>
/// <remarks>
/// <para>
/// <b>No session is ever passed.</b> The hosted agent is a singleton shared across customers, so a session
/// carried between runs would carry one customer's request into the next one's answer.
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
        var failed = false;

        await foreach (var raised in Relay(agent, payload.Json, text, cancellationToken))
        {
            failed |= raised is WorkspaceSuggestionUnavailableAgentEvent;

            yield return raised;
        }

        if (failed)
        {
            yield break;
        }

        var answer = MicrosoftFoundrySuggestionAnswerReader.ReadAnswer(text.ToString());

        if (answer is null)
        {
            yield return new WorkspaceSuggestionUnavailableAgentEvent(NotTheContract);
            yield break;
        }

        yield return new WorkspaceSuggestionResultReadyAgentEvent(answer, payload.Hash);
    }

    /// <summary>What the customer is told when the answer is not the shape the contract promised.</summary>
    private const string NotTheContract = "The agent's answer was not the expected contract.";

    /// <summary>Passes the agent's text on as it arrives, and reads nothing into it.</summary>
    private static async IAsyncEnumerable<WorkspaceSuggestionAgentEvent> Relay(
        AIAgent agent,
        string json,
        StringBuilder text,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await using var updates = agent
            .RunStreamingAsync(json, cancellationToken: cancellationToken)
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
