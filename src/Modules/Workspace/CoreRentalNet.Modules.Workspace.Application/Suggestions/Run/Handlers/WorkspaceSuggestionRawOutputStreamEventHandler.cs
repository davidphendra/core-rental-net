using CoreRentalNet.Modules.Workspace.Application.Suggestions.Agent;
using CoreRentalNet.Modules.Workspace.Application.Suggestions.Agent.Events;
using CoreRentalNet.Modules.Workspace.Application.Suggestions.Run.Events;

namespace CoreRentalNet.Modules.Workspace.Application.Suggestions.Run.Handlers;

/// <summary>Keeps the agent's own words for the run's record, and shows none of them.</summary>
/// <remarks>
/// The raw output is the run's evidence rather than its presentation: a rejected run is the one worth debugging,
/// and the model's own answer is the only thing that can tell a bad prompt from a bad catalogue. Nothing here
/// produces a frame, because the customer reads the application's words and never the model's.
/// </remarks>
public sealed class WorkspaceSuggestionRawOutputStreamEventHandler : IWorkspaceSuggestionStreamEventHandler
{
    public bool TryHandleAgentEvent(
        WorkspaceSuggestionEvent suggestionEvent,
        WorkspaceSuggestionRunState suggestionRunState,
        ICollection<WorkspaceSuggestionStreamEventBase> frames)
    {
        if (suggestionEvent is not WorkspaceSuggestionRawOutputEvent rawOutputAgentEvent)
        {
            return false;
        }

        suggestionRunState.AppendRawOutput(rawOutputAgentEvent.RawText);

        return true;
    }
}
