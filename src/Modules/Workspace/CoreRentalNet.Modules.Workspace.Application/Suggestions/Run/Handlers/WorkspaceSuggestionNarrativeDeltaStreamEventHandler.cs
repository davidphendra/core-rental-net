using CoreRentalNet.Modules.Workspace.Application.Suggestions.Agent;
using CoreRentalNet.Modules.Workspace.Application.Suggestions.Agent.Events;
using CoreRentalNet.Modules.Workspace.Application.Suggestions.Run.Events;
using CoreRentalNet.Modules.Workspace.Application.Suggestions.Reading;

namespace CoreRentalNet.Modules.Workspace.Application.Suggestions.Run.Handlers;

/// <summary>Streams the model's own words, a finished field at a time, and never its JSON.</summary>
public sealed class WorkspaceSuggestionNarrativeDeltaStreamEventHandler : IWorkspaceSuggestionStreamEventHandler
{
    public bool TryHandleAgentEvent(
        WorkspaceSuggestionEvent suggestionEvent,
        WorkspaceSuggestionRunState suggestionRunState,
        ICollection<WorkspaceSuggestionStreamEvent> frames)
    {
        if (suggestionEvent is not WorkspaceSuggestionNarrativeDeltaEvent narrativeDeltaAgentEvent)
        {
            return false;
        }

        // A delta closes no field until one is complete, so the frames this reports are often none at all - and
        // that is an answer rather than a pass, which is why the return below is outside the loop.
        foreach (var narrativeField in suggestionRunState.AppendNarrativeText(narrativeDeltaAgentEvent.NarrativeText))
        {
            frames.Add(new WorkspaceSuggestionTextStreamEvent(
                WorkspaceSuggestionOutputHygiene.Apply(narrativeField.Kind, narrativeField.Text)));
        }

        return true;
    }
}
