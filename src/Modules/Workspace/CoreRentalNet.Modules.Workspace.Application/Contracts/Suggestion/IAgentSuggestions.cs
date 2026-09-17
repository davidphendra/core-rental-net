namespace CoreRentalNet.Modules.Workspace.Application.Contracts.Suggestion;

/// <summary>
/// Asks the agent for candidates, streaming what it says as it says it. The one thing in this module
/// that leaves the application.
/// </summary>
/// <remarks>
/// A port because the caller must be testable without an agent, and because where the agent runs is a
/// deployment decision. It is also the seam that keeps the Host adapter - and the credential it carries
/// - out of this layer.
/// </remarks>
public interface IAgentSuggestions
{
    /// <summary>
    /// Whether an agent is configured at all.
    /// </summary>
    /// <remarks>
    /// Asked before anything else, because "no agent is configured" and "the agent did not answer" mean
    /// the same thing to a customer and neither is a refusal. A deployment that has not configured one
    /// should not be asked to reach one to find out.
    /// </remarks>
    bool IsConfigured { get; }

    IAsyncEnumerable<AgentSuggestionMessage> AskAsync(string query, CancellationToken cancellationToken = default);
}
