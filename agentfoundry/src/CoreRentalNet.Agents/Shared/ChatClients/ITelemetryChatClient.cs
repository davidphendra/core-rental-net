using Microsoft.Extensions.AI;
using CoreRentalNet.Agents.Shared.Model;

namespace CoreRentalNet.Agents.Shared.ChatClients;

/// <summary>What a caller needs of the run's telemetry: a client it can compose, the run's cost, and its span.</summary>
/// <remarks>
/// A port rather than the class, so the stage agents and the completion executors name what they use and not the
/// concrete client that observes it. The implementation is the sealed <see cref="TelemetryChatClient"/>.
/// It extends <see cref="IChatClient"/> because the agent builder composes it as one; <see cref="Total"/> and the
/// run span are what it adds.
/// </remarks>
internal interface ITelemetryChatClient : IChatClient
{
    /// <summary>The chat options property a stage's agent sets to name itself, so its call is attributed to it.</summary>
    const string AgentName = "telemetry.agent.name";

    /// <summary>What the run has cost so far.</summary>
    AgentRunUsage Total { get; }

    /// <summary>
    /// Opens the run's span. Called by the node that reads the request, which is still inside the HTTP request, so
    /// the span is parented to the platform's server span through the ambient activity.
    /// </summary>
    void StartRun(string runId);

    /// <summary>
    /// Closes the run's span and counts the run. Called by whichever completion node ended it, because that is the
    /// only place the ending is known for certain.
    /// </summary>
    void CompleteRun(string runStatus);
}
