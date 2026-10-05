using CoreRentalNet.Modules.Workspace.Application.Suggestions.Agent;
using CoreRentalNet.Modules.Workspace.Application.Suggestions.Agent.Events;
using CoreRentalNet.Modules.Workspace.Application.Suggestions.Run.Events;

namespace CoreRentalNet.Modules.Workspace.Application.Suggestions.Run.Handlers;

/// <summary>Reports the agent's stage as finished, in the application's words, so the panel can close it.</summary>
public sealed class WorkspaceSuggestionStageCompletedStreamEventHandler : IWorkspaceSuggestionStreamEventHandler
{
    public bool TryHandleAgentEvent(
        WorkspaceSuggestionEvent suggestionEvent,
        WorkspaceSuggestionRunState suggestionRunState,
        ICollection<WorkspaceSuggestionStreamEventBase> frames)
    {
        if (suggestionEvent is not WorkspaceSuggestionStageCompletedEvent stageCompletedAgentEvent)
        {
            return false;
        }

        frames.Add(new WorkspaceSuggestionStageCompletedStreamEvent(
            WorkspaceSuggestionStage.WordsFor(stageCompletedAgentEvent.AgentProcessingStage)));

        return true;
    }
}
