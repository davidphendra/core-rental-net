using CoreRentalNet.Modules.Workspace.Application.Suggestions.Agent;
using CoreRentalNet.Modules.Workspace.Application.Suggestions.Agent.Events;
using CoreRentalNet.Modules.Workspace.Application.Suggestions.Run.Events;

namespace CoreRentalNet.Modules.Workspace.Application.Suggestions.Run.Handlers;

/// <summary>Reports a run that could not be made, and nothing derived from the agent's own reason.</summary>
public sealed class WorkspaceSuggestionUnavailableStreamEventHandler : IWorkspaceSuggestionStreamEventHandler
{
    public bool TryHandleAgentEvent(
        WorkspaceSuggestionEvent suggestionEvent,
        WorkspaceSuggestionRunState suggestionRunState,
        ICollection<WorkspaceSuggestionStreamEvent> frames)
    {
        if (suggestionEvent is not WorkspaceSuggestionUnavailableEvent)
        {
            return false;
        }

        suggestionRunState.MarkUnavailable();

        frames.Add(new WorkspaceSuggestionFailedStreamEvent(WorkspaceSuggestionFailureCode.Unavailable));

        return true;
    }
}
