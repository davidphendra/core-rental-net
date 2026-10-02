using System.Runtime.CompilerServices;
using CoreRentalNet.Modules.Workspace.Application.Suggestions.Agent;
using CoreRentalNet.Modules.Workspace.Application.Suggestions.Agent.Events;
using CoreRentalNet.Modules.Workspace.Application.Suggestions.Run.Events;
using CoreRentalNet.Modules.Workspace.Application.Suggestions.Run.Handlers;

namespace CoreRentalNet.Modules.Workspace.Application.Suggestions.Run;

/// <summary>Applies each agent event to one run, through the handler that owns it.</summary>
/// <remarks>
/// A new event kind is a new handler registered beside the others; this type is not edited to admit one. It
/// yields what the handler produced rather than handing it a writer, so the same pipeline serves a test that
/// reads frames and an endpoint that writes them.
/// </remarks>
public sealed class WorkspaceSuggestionStreamProcessor(
    IEnumerable<IWorkspaceSuggestionStreamEventHandler> suggestionStreamEventHandlers)
    : IWorkspaceSuggestionStreamProcessor
{
    public async IAsyncEnumerable<WorkspaceSuggestionStreamEvent> ProcessAsync(
        IAsyncEnumerable<WorkspaceSuggestionEvent> suggestionAgentEvents,
        WorkspaceSuggestionRunState suggestionRunState,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await foreach (var suggestionAgentEvent in suggestionAgentEvents.WithCancellation(cancellationToken))
        {
            var frames = new List<WorkspaceSuggestionStreamEvent>();

            foreach (var suggestionStreamEventHandler in suggestionStreamEventHandlers)
            {
                if (suggestionStreamEventHandler.TryHandleAgentEvent(
                        suggestionAgentEvent,
                        suggestionRunState,
                        frames))
                {
                    break;
                }
            }

            foreach (var frame in frames)
            {
                yield return frame;
            }

            if (suggestionRunState.HasEnded)
            {
                break;
            }
        }
    }
}
