using CoreRentalNet.Modules.Workspace.Application.Suggestions.Agent;

namespace CoreRentalNet.Modules.Workspace.Application.Suggestions.Run;

/// <summary>Writes a retry as a stage-like line, because it is where the run went and not what it produced.</summary>
public sealed class WorkspaceSuggestionRetryStreamEventHandler : IWorkspaceSuggestionStreamEventHandler
{
    public async Task<bool> TryHandleAgentEventAsync(
        WorkspaceSuggestionAgentEvent suggestionAgentEvent,
        WorkspaceSuggestionRunState suggestionRunState,
        IWorkspaceSuggestionEventWriter suggestionEventWriter,
        CancellationToken cancellationToken)
    {
        if (suggestionAgentEvent is not WorkspaceSuggestionRetryAgentEvent retryAgentEvent)
        {
            return false;
        }

        await suggestionEventWriter.WriteAsync(
            new WorkspaceSuggestionRetryStreamEvent(
                $"Reconsidering your request (attempt {retryAgentEvent.NextAttemptNumber} of {retryAgentEvent.MaximumAttemptCount})"),
            cancellationToken);

        return true;
    }
}
