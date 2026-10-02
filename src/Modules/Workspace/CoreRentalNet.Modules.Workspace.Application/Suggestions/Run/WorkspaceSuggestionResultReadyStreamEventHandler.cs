using CoreRentalNet.Modules.Workspace.Application.Suggestions.Agent;
using CoreRentalNet.Modules.Workspace.Application.Suggestions.Agent.Events;
using CoreRentalNet.Modules.Workspace.Application.Suggestions.Run.Events;
using CoreRentalNet.Modules.Workspace.Domain;

namespace CoreRentalNet.Modules.Workspace.Application.Suggestions.Run;

/// <summary>Reports the terminal answer, whole or not at all.</summary>
public sealed class WorkspaceSuggestionResultReadyStreamEventHandler : IWorkspaceSuggestionStreamEventHandler
{
    public bool TryHandleAgentEvent(
        WorkspaceSuggestionEvent suggestionEvent,
        WorkspaceSuggestionRunState suggestionRunState,
        ICollection<WorkspaceSuggestionStreamEvent> frames)
    {
        if (suggestionEvent is not WorkspaceSuggestionResultReadyEvent resultReadyAgentEvent)
        {
            return false;
        }

        var resultFrame = suggestionRunState.CompleteWithAgentAnswer(resultReadyAgentEvent);

        // A run that streamed no frame is worded by how it ended: an unavailable catalogue is not an answer
        // this application could not honour, and the customer is told the difference.
        frames.Add(resultFrame is null
            ? new WorkspaceSuggestionFailedStreamEvent(
                suggestionRunState.Verdict is WorkspaceSuggestionVerdict.Unavailable
                    ? WorkspaceSuggestionFailureCode.Unavailable
                    : WorkspaceSuggestionFailureCode.Invalid)
            : new WorkspaceSuggestionResultStreamEvent(resultFrame));

        return true;
    }
}
