using CoreRentalNet.Modules.Workspace.Application.Suggestions.Agent;

namespace CoreRentalNet.Modules.Workspace.Application.Suggestions.Run;

/// <summary>Writes the terminal answer, whole or not at all.</summary>
public sealed class WorkspaceSuggestionResultReadyStreamEventHandler : IWorkspaceSuggestionStreamEventHandler
{
    public async Task<bool> TryHandleAgentEventAsync(
        WorkspaceSuggestionAgentEvent suggestionAgentEvent,
        WorkspaceSuggestionRunState suggestionRunState,
        IWorkspaceSuggestionEventWriter suggestionEventWriter,
        CancellationToken cancellationToken)
    {
        if (suggestionAgentEvent is not WorkspaceSuggestionResultReadyAgentEvent resultReadyAgentEvent)
        {
            return false;
        }

        var resultFrame = suggestionRunState.CompleteWithAgentAnswer(resultReadyAgentEvent);

        await suggestionEventWriter.WriteAsync(
            resultFrame is null
                ? new WorkspaceSuggestionFailedStreamEvent(WorkspaceSuggestionFailureCode.Invalid)
                : new WorkspaceSuggestionResultStreamEvent(resultFrame),
            cancellationToken);

        return true;
    }
}
