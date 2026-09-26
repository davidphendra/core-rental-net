using CoreRentalNet.Modules.Workspace.Application.Suggestions.Agent;

namespace CoreRentalNet.Modules.Workspace.Application.Suggestions.Run;

/// <summary>The one place a suggestion run is executed: the service the endpoint delegates to.</summary>
public interface IWorkspaceSuggestionRunService
{
    Task RunSuggestionAsync(
        WorkspaceSuggestionRequestPayload suggestionRequestPayload,
        IWorkspaceSuggestionEventWriter suggestionEventWriter,
        string hashedCustomerIdentity,
        CancellationToken cancellationToken);
}
