using CoreRentalNet.Modules.Workspace.Application.Suggestions.Agent;

namespace CoreRentalNet.Modules.Workspace.Application.Suggestions.Run;

/// <summary>Applies each agent event to one run, through the handler that owns it.</summary>
/// <remarks>
/// A new event kind is a new handler registered beside the others; this type is not edited to admit one.
/// </remarks>
public sealed class WorkspaceSuggestionStreamProcessor(
    IEnumerable<IWorkspaceSuggestionStreamEventHandler> suggestionStreamEventHandlers)
    : IWorkspaceSuggestionStreamProcessor
{
    public async Task ProcessAgentEventStreamAsync(
        IAsyncEnumerable<WorkspaceSuggestionAgentEvent> suggestionAgentEvents,
        WorkspaceSuggestionRunState suggestionRunState,
        IWorkspaceSuggestionEventWriter suggestionEventWriter,
        CancellationToken cancellationToken)
    {
        await foreach (var suggestionAgentEvent in suggestionAgentEvents.WithCancellation(cancellationToken))
        {
            foreach (var suggestionStreamEventHandler in suggestionStreamEventHandlers)
            {
                if (await suggestionStreamEventHandler.TryHandleAgentEventAsync(
                        suggestionAgentEvent,
                        suggestionRunState,
                        suggestionEventWriter,
                        cancellationToken))
                {
                    break;
                }
            }

            if (suggestionRunState.HasEnded)
            {
                break;
            }
        }
    }
}
