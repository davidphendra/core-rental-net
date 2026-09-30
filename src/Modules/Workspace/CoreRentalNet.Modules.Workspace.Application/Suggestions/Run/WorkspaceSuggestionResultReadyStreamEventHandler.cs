using CoreRentalNet.Modules.Workspace.Application.Suggestions.Agent;
using CoreRentalNet.Modules.Workspace.Domain;

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

        // A run that streamed no frame is worded by how it ended: an unavailable catalogue is not an answer
        // this application could not honour, and the customer is told the difference.
        await suggestionEventWriter.WriteAsync(
            resultFrame is null
                ? new WorkspaceSuggestionFailedStreamEvent(
                    suggestionRunState.Verdict is WorkspaceSuggestionVerdict.Unavailable
                        ? WorkspaceSuggestionFailureCode.Unavailable
                        : WorkspaceSuggestionFailureCode.Invalid)
                : new WorkspaceSuggestionResultStreamEvent(resultFrame),
            cancellationToken);

        return true;
    }
}
