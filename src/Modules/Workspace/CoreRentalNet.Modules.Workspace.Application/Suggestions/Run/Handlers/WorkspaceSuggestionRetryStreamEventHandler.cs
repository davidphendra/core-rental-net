using CoreRentalNet.Modules.Workspace.Application.Suggestions.Agent;
using CoreRentalNet.Modules.Workspace.Application.Suggestions.Agent.Events;
using CoreRentalNet.Modules.Workspace.Application.Suggestions.Run.Events;

namespace CoreRentalNet.Modules.Workspace.Application.Suggestions.Run.Handlers;

/// <summary>Reports a retry as a stage-like line, because it is where the run went and not what it produced.</summary>
public sealed class WorkspaceSuggestionRetryStreamEventHandler : IWorkspaceSuggestionStreamEventHandler
{
    public bool TryHandleAgentEvent(
        WorkspaceSuggestionEvent suggestionEvent,
        WorkspaceSuggestionRunState suggestionRunState,
        ICollection<WorkspaceSuggestionStreamEvent> frames)
    {
        if (suggestionEvent is not WorkspaceSuggestionRetryEvent retryAgentEvent)
        {
            return false;
        }

        frames.Add(new WorkspaceSuggestionRetryStreamEvent(
            $"Reconsidering your request (attempt {retryAgentEvent.NextAttemptNumber} of {retryAgentEvent.MaximumAttemptCount})"));

        return true;
    }
}
