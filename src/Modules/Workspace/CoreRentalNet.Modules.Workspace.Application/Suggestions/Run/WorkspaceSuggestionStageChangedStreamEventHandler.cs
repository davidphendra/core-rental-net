using CoreRentalNet.Modules.Workspace.Application.Suggestions.Agent;
using CoreRentalNet.Modules.Workspace.Application.Suggestions.Agent.Events;
using CoreRentalNet.Modules.Workspace.Application.Suggestions.Run.Events;

namespace CoreRentalNet.Modules.Workspace.Application.Suggestions.Run;

/// <summary>Reports the agent's stage, in the application's words, as a stage frame.</summary>
public sealed class WorkspaceSuggestionStageChangedStreamEventHandler : IWorkspaceSuggestionStreamEventHandler
{
    public bool TryHandleAgentEvent(
        WorkspaceSuggestionEvent suggestionEvent,
        WorkspaceSuggestionRunState suggestionRunState,
        ICollection<WorkspaceSuggestionStreamEvent> frames)
    {
        if (suggestionEvent is not WorkspaceSuggestionChangedEvent stageChangedAgentEvent)
        {
            return false;
        }

        frames.Add(new WorkspaceSuggestionStageStreamEvent(
            WorkspaceSuggestionStage.WordsFor(stageChangedAgentEvent.AgentProcessingStage)));

        return true;
    }
}
