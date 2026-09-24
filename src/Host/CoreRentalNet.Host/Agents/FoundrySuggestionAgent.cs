using System.Runtime.CompilerServices;
using System.Text;
using CoreRentalNet.Host.AiBuilder;
using Microsoft.Agents.AI;

namespace CoreRentalNet.Host.Agents;

/// <summary>The real adapter: it calls the hosted agent and turns its answer into the application's events.</summary>
/// <remarks>
/// <para>
/// <b>No session is ever passed.</b> The hosted agent is a singleton shared across customers, so a session
/// carried between runs would carry one customer's request into the next one's answer. There is no session
/// argument here and no session field on this class, because there is nowhere for one to live.
/// </para>
/// <para>
/// <summary>
/// The answer is read as a sequence of values, not one document — see <see cref="ResponseObjects"/> for why
/// the object to read is chosen by what it carries rather than by where it sits.
/// </para>
/// <para>
/// A failure the adapter can see becomes <c>Unavailable</c>, which the endpoint reports and the customer
/// retries. A refusal is not a failure and arrives as <c>Completed</c> carrying the verdict.
/// </para>
/// </remarks>
internal sealed class FoundrySuggestionAgent : ISuggestionAgent
{
    private readonly AgentFoundrySettings _settings;
    private readonly Func<AgentFoundrySettings, AIAgent> _agent;

    public FoundrySuggestionAgent(AgentFoundrySettings settings)
        : this(settings, FoundryAgentFactory.Build)
    {
    }

    /// <summary>The agent is built by a delegate so a test can supply one over a fake model client.</summary>
    internal FoundrySuggestionAgent(AgentFoundrySettings settings, Func<AgentFoundrySettings, AIAgent> agent)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(agent);

        _settings = settings;
        _agent = agent;
    }

    public async IAsyncEnumerable<AgentSuggestionEvent> StreamAsync(
        SuggestionRequest request,
        RunBudget budget)
    {
        ArgumentNullException.ThrowIfNull(budget);

        var payload = SuggestionPayload.From(request);
        var agent = TryCreate(out var creationFailure);

        if (agent is null)
        {
            yield return new AgentSuggestionEvent.Unavailable(creationFailure!);
            yield break;
        }

        var text = new StringBuilder();
        var failed = false;

        await foreach (var raised in Relay(agent, payload.Json, text, budget))
        {
            failed |= raised is AgentSuggestionEvent.Unavailable;

            yield return raised;
        }

        if (failed)
        {
            yield break;
        }

        var (result, runUsage) = Read(text.ToString());

        if (result is null)
        {
            yield return new AgentSuggestionEvent.Unavailable(NotTheContract);
            yield break;
        }

        yield return new AgentSuggestionEvent.Completed(result with { RunUsage = runUsage }, payload.Hash);
    }

    /// <summary>What the customer is told when the answer is not the shape the contract promised.</summary>
    private const string NotTheContract = "The agent's answer was not the expected contract.";

    /// <summary>The typed result and the run's cost, each found by what it carries rather than by where it sits.</summary>
    /// <remarks>
    /// <b>Not by position.</b> The cost is appended <em>after</em> the result, so reading the last object
    /// would read the cost as the answer, and reading the first would read the rephraser's specification.
    /// And not by "does it deserialize": an enum with a missing value deserializes to its first member, so an
    /// object carrying no status at all would quietly pass as a suggestion.
    /// </remarks>
    private static (AgentSuggestionResult? Result, AgentRunUsage? RunUsage) Read(string text)
    {
        var objects = ResponseObjects.All(text);

        var result = objects
            .Where(element => ResponseObjects.Has(element, "status"))
            .Select(ResponseObjects.Read<AgentSuggestionResult>)
            .LastOrDefault(candidate => candidate is not null);

        var runUsage = objects
            .Where(element => ResponseObjects.Has(element, "runUsage"))
            .Select(ResponseObjects.Read<AgentRunUsageReport>)
            .LastOrDefault(report => report?.RunUsage is not null)
            ?.RunUsage;

        return (result, runUsage);
    }

    /// <summary>Passes the agent's text on as it arrives, and reads nothing into it.</summary>
    /// <remarks>
    /// The two tokens travel together in the budget so this method cannot be called with them swapped — which
    /// would report a timeout as a stop and a stop as a timeout, and would pass any test that only looked at the
    /// name of the outcome.
    /// </remarks>
    private static async IAsyncEnumerable<AgentSuggestionEvent> Relay(
        AIAgent agent,
        string json,
        StringBuilder text,
        RunBudget budget)
    {
        await using var updates = agent
            .RunStreamingAsync(json, cancellationToken: budget.Run)
            .GetAsyncEnumerator(budget.Run);

        while (true)
        {
            // A yield cannot sit inside a try that catches, so the failure is recorded and reported just after.
            bool moved = false;
            string? failure = null;

            try
            {
                moved = await updates.MoveNextAsync();
            }
            catch (OperationCanceledException) when (budget.Customer.IsCancellationRequested)
            {
                // The customer stopped the run, or their browser went away. That is an <b>ending</b>, not an
                // unavailable run, so it is rethrown as itself rather than dressed up as one - the page
                // reports a different thing for each. The two tokens are not the same question: this one
                // says the customer left, and the budget's says the run ran out of time, which IS a failure.
                throw;
            }
            catch (Exception exception)
            {
                failure = exception.Message;
            }

            if (failure is not null)
            {
                yield return new AgentSuggestionEvent.Unavailable(failure);
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

            yield return new AgentSuggestionEvent.NarrativeDelta(fragment);
        }
    }

    private AIAgent? TryCreate(out string? failure)
    {
        failure = null;

        try
        {
            return _agent(_settings);
        }
        catch (Exception exception)
        {
            failure = exception.Message;

            return null;
        }
    }
}
