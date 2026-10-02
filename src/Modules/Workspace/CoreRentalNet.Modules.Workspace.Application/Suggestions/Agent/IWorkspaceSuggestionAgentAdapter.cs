using CoreRentalNet.Modules.Workspace.Application.Suggestions.Agent.Events;

namespace CoreRentalNet.Modules.Workspace.Application.Suggestions.Agent;

/// <summary>The application's only knowledge of the agent, so a provider never leaks inward.</summary>
/// <remarks>
/// Its shape is the fake's shape. An <b>unavailable</b> agent and a <b>refusal</b> are different outcomes and
/// arrive as different events: the first is a failure the customer retries, the second is an answer the
/// application words. Nothing here names Microsoft Foundry, Azure or OpenAI: the adapter that does lives in
/// the module's infrastructure and maps the provider's answer onto <see cref="WorkspaceSuggestionAnswer"/>.
/// </remarks>
public interface IWorkspaceSuggestionAgentAdapter
{
    /// <summary>Runs one suggestion, yielding what happens as it happens.</summary>
    /// <param name="cancellationToken">
    /// The customer's own token. <b>The application sets no deadline of its own</b> - how long a run may take
    /// is the agent's to decide, so this is signalled only when the customer stops or their connection goes
    /// away, which is an ending rather than a failure.
    /// </param>
    IAsyncEnumerable<WorkspaceSuggestionEvent> StreamAsync(
        WorkspaceSuggestionRequestPayload suggestionRequestPayload,
        CancellationToken cancellationToken);
}
