using CoreRentalNet.Modules.Workspace.Application.Suggestions.Agent;
using CoreRentalNet.Modules.Workspace.Application.Suggestions.Agent.Events;
using CoreRentalNet.Modules.Workspace.Application.Suggestions.Run.Events;

namespace CoreRentalNet.Modules.Workspace.Application.Suggestions.Run.Handlers;

/// <summary>Reports the agent's stage, in the application's words, as a stage frame.</summary>
public sealed class WorkspaceSuggestionStageStartedStreamEventHandler : IWorkspaceSuggestionStreamEventHandler
{
    public bool TryHandleAgentEvent(
        WorkspaceSuggestionEvent suggestionEvent,
        WorkspaceSuggestionRunState suggestionRunState,
        ICollection<WorkspaceSuggestionStreamEventBase> frames)
    {
        if (suggestionEvent is not WorkspaceSuggestionStageStartedEvent stageStartedAgentEvent)
        {
            return false;
        }

        frames.Add(new WorkspaceSuggestionStageStartedStreamEvent(
            WorkspaceSuggestionStage.WordsFor(stageStartedAgentEvent.AgentProcessingStage)));

        return true;
    }
}
