using System.Runtime.CompilerServices;
using System.Text;
using CoreRentalNet.Modules.Workspace.Application.Suggestions.Agent;
using CoreRentalNet.Modules.Workspace.Application.Suggestions.Agent.Events;
using Microsoft.Agents.AI;
using Microsoft.Extensions.Hosting;

namespace CoreRentalNet.Modules.Workspace.Infrastructure.Suggestions;

/// <summary>The real adapter: it calls the hosted agent and turns its answer into the application's events.</summary>
/// <remarks>
/// <para>
/// <b>Every run gets a session of its own, and none of them is reused.</b> The hosted agent is a singleton shared
/// across customers, so a session carried between runs would carry one customer's request into the next one's
/// answer. A fresh, empty session per run is what makes that impossible rather than unlikely — and nothing is ever
/// written into one.
/// </para>
/// <para>
/// <b>Progress is read as it arrives; the ending is read at the end.</b> The agent streams a typed event per line,
/// so a stage, a retry and an approved setup become the application's events the moment their object closes — which
/// is what lets the panel show where a run is while it runs. The whole text is kept for the ending, because the
/// ending is in none of its fragments, and nothing is known about the run's outcome until its <c>completed</c>
/// event arrives.
/// </para>
/// <para>
/// A failure the adapter can see becomes <c>Unavailable</c>, which the endpoint reports and the customer
/// retries. A refusal is not a failure and arrives as <c>ResultReady</c> carrying the answer.
/// </para>
/// </remarks>
public sealed class AgentFoundryWorkspaceSuggestionAdapter : IWorkspaceSuggestionAgentAdapter
{
    private readonly AgentFoundryConnectionSetting _connectionSetting;
    private readonly IWorkspaceSuggestionProgressReader _progressReader;
    private readonly IHostEnvironment _hostEnvironment;

    public AgentFoundryWorkspaceSuggestionAdapter(
        AgentFoundryConnectionSetting connectionSetting,
        IWorkspaceSuggestionProgressReader progressReader,
        IHostEnvironment hostEnvironment)
    {
        ArgumentNullException.ThrowIfNull(connectionSetting);
        ArgumentNullException.ThrowIfNull(progressReader);
        ArgumentNullException.ThrowIfNull(hostEnvironment);

        _connectionSetting = connectionSetting;
        _progressReader = progressReader;
        _hostEnvironment = hostEnvironment;
    }

    /// <summary>Translates the agent's streamed text into the application's events, as the text arrives.</summary>
    /// <remarks>
    /// <b>The agent does not emit a <see cref="WorkspaceSuggestionEvent"/>; it emits text.</b> What arrives is a
    /// stream of fragments cut by the transport rather than by the protocol, so a fragment can end in the middle of
    /// a token. This method is the translation boundary — fragments in, the application's own events out — and the
    /// text is both read for progress as it arrives and kept whole, because progress is in a fragment and the
    /// ending is in the whole text.
    /// </remarks>
    public async IAsyncEnumerable<WorkspaceSuggestionEvent> StreamAsync(
        WorkspaceSuggestionRequestPayload suggestionRequestPayload,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var requestPayload = AgentFoundrySuggestionRequestPayload.From(suggestionRequestPayload);
        var agentClient = AgentFoundryWorkspaceSuggestionAdapterFactory.Build(_connectionSetting, _hostEnvironment);
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

    /// <summary>What the streamed text amounts to: its ending, once the stream has stopped.</summary>
    /// <remarks>
    /// <b>Reading the whole text belongs here rather than to the relay.</b> A <c>yield</c> cannot sit inside a
    /// <c>try</c> that catches, and reading text a model wrote can fail; the tail is a pure synchronous function of
    /// a string, so it is testable without an agent, a transport or a cancellation. Progress is deliberately not
    /// read here: it was read as each object closed, so replaying it now would say where the run had been only
    /// after it had already arrived.
    /// </remarks>
    private static IEnumerable<WorkspaceSuggestionEvent> ParseFragment(string text, string payloadHash)
    {
        var streamedAnswer = WorkspaceSuggestionFragmentReader.ReadAnswer(text);

        if (streamedAnswer is null)
        {
            yield return new WorkspaceSuggestionUnavailableEvent(NotTheContract);
            yield break;
        }

        yield return new WorkspaceSuggestionResultReadyEvent(streamedAnswer, payloadHash);
    }

    /// <summary>What the customer is told when the answer is not the shape the contract promised.</summary>
    private const string NotTheContract = "The agent's answer was not the expected contract.";

    /// <summary>Relays one agent's updates, keeping the text and reading the progress each closes.</summary>
    private async IAsyncEnumerable<WorkspaceSuggestionEvent> RelayUpdateStream(
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

            foreach (var raised in EventsOf(fragment, agentUpdateBuilder))
            {
                yield return raised;
            }
        }
    }

    /// <summary>One fragment: its words for the record, then any progress its closing object just made readable.</summary>
    private IEnumerable<WorkspaceSuggestionEvent> EventsOf(string fragment, StringBuilder agentUpdateBuilder)
    {
        agentUpdateBuilder.Append(fragment);

        yield return new WorkspaceSuggestionRawOutputEvent(fragment);

        foreach (var progressEvent in _progressReader.Read(agentUpdateBuilder.ToString()))
        {
            yield return progressEvent;
        }
    }
}
