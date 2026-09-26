using CoreRentalNet.Modules.Workspace.Application.Suggestions.Agent;

namespace CoreRentalNet.Modules.Workspace.Application.Suggestions.Run;

/// <summary>Applies an agent's whole stream to one run, through the chain of handlers.</summary>
public interface IWorkspaceSuggestionStreamProcessor
{
    Task ProcessAgentEventStreamAsync(
        IAsyncEnumerable<WorkspaceSuggestionAgentEvent> suggestionAgentEvents,
        WorkspaceSuggestionRunState suggestionRunState,
        IWorkspaceSuggestionEventWriter suggestionEventWriter,
        CancellationToken cancellationToken);
}
