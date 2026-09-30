using CoreRentalNet.Modules.Workspace.Application.Suggestions.Agent;

namespace CoreRentalNet.Modules.Workspace.Application.Suggestions.Run;

/// <summary>Writes the agent's stage, in the application's words, as a stage frame.</summary>
public sealed class WorkspaceSuggestionStageChangedStreamEventHandler : IWorkspaceSuggestionStreamEventHandler
{
    public async Task<bool> TryHandleAgentEventAsync(
        WorkspaceSuggestionAgentEvent suggestionAgentEvent,
        WorkspaceSuggestionRunState suggestionRunState,
        IWorkspaceSuggestionEventWriter suggestionEventWriter,
        CancellationToken cancellationToken)
    {
        if (suggestionAgentEvent is not WorkspaceSuggestionStageChangedAgentEvent stageChangedAgentEvent)
        {
            return false;
        }

        await suggestionEventWriter.WriteAsync(
            new WorkspaceSuggestionStageStreamEvent(
                WorkspaceSuggestionStage.WordsFor(stageChangedAgentEvent.AgentProcessingStage)),
            cancellationToken);

        return true;
    }
}
