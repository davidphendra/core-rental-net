using CoreRentalNet.Modules.Workspace.Application.Suggestions.Agent;

namespace CoreRentalNet.Modules.Workspace.Application.Suggestions.Run;

/// <summary>Reports a run that could not be made, and nothing derived from the agent's own reason.</summary>
public sealed class WorkspaceSuggestionUnavailableStreamEventHandler : IWorkspaceSuggestionStreamEventHandler
{
    public async Task<bool> TryHandleAgentEventAsync(
        WorkspaceSuggestionAgentEvent suggestionAgentEvent,
        WorkspaceSuggestionRunState suggestionRunState,
        IWorkspaceSuggestionEventWriter suggestionEventWriter,
        CancellationToken cancellationToken)
    {
        if (suggestionAgentEvent is not WorkspaceSuggestionUnavailableAgentEvent)
        {
            return false;
        }

        suggestionRunState.MarkUnavailable();

        await suggestionEventWriter.WriteAsync(
            new WorkspaceSuggestionFailedStreamEvent(WorkspaceSuggestionFailureCode.Unavailable),
            cancellationToken);

        return true;
    }
}
