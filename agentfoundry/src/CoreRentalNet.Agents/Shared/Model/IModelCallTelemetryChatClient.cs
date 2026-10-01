using Microsoft.Extensions.AI;

namespace CoreRentalNet.Agents.Shared.Model;

/// <summary>What a caller needs of the run's telemetry: a client it can compose, and the run's cost.</summary>
/// <remarks>
/// A port rather than the class, so the stage agents and the completion executors name what they use and not the
/// concrete client that observes it. The implementation is the sealed <see cref="ModelCallTelemetryChatClient"/>.
/// It extends <see cref="IChatClient"/> because the agent builder composes it as one; <see cref="Total"/> is the
/// only thing it adds.
/// </remarks>
internal interface IModelCallTelemetryChatClient : IChatClient
{
    /// <summary>The chat options property a stage's agent sets to name itself, so its call is attributed to it.</summary>
    const string AgentName = "telemetry.agent.name";

    /// <summary>What the run has cost so far.</summary>
    AgentRunUsage Total { get; }
}
