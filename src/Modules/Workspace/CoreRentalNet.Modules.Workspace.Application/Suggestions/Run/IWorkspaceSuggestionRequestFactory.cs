using CoreRentalNet.Modules.Workspace.Application.Suggestions.Agent;

namespace CoreRentalNet.Modules.Workspace.Application.Suggestions.Run;

/// <summary>Builds the one payload a run sends: the customer's sentence and the slot rules.</summary>
public interface IWorkspaceSuggestionRequestFactory
{
    WorkspaceSuggestionRequestPayload Create(WorkspaceSuggestionQuery suggestionQuery, string accessToken);
}
