using CoreRentalNet.Modules.Workspace.Application.Contracts.Suggestion;

namespace CoreRentalNet.Modules.Workspace.Application.Services;

/// <summary>
/// Asks the agent for workspace candidates, and returns only what the application can stand behind.
/// </summary>
/// <remarks>
/// The application's own operation rather than the agent's: what it answers has been checked against the
/// catalogue, and a caller never sees a SKU that does not exist or a price the agent stated.
/// </remarks>
public interface ISuggestWorkspaceOptions
{
    Task<WorkspaceSuggestion> SuggestAsync(
        WorkspaceSuggestionRequest request,
        CancellationToken cancellationToken = default);
}
