using CoreRentalNet.Modules.Workspace.Application.Suggestions.Agent;
using CoreRentalNet.Modules.Workspace.Application.Suggestions.Reading;

namespace CoreRentalNet.Modules.Workspace.Application.Suggestions.Run;

/// <summary>Streams the model's own words, a finished field at a time, and never its JSON.</summary>
public sealed class WorkspaceSuggestionNarrativeDeltaStreamEventHandler : IWorkspaceSuggestionStreamEventHandler
{
    public async Task<bool> TryHandleAgentEventAsync(
        WorkspaceSuggestionAgentEvent suggestionAgentEvent,
        WorkspaceSuggestionRunState suggestionRunState,
        IWorkspaceSuggestionEventWriter suggestionEventWriter,
        CancellationToken cancellationToken)
    {
        if (suggestionAgentEvent is not WorkspaceSuggestionNarrativeDeltaAgentEvent narrativeDeltaAgentEvent)
        {
            return false;
        }

        foreach (var narrativeField in suggestionRunState.AppendNarrativeText(narrativeDeltaAgentEvent.NarrativeText))
        {
            await suggestionEventWriter.WriteAsync(
                new WorkspaceSuggestionTextStreamEvent(
                    WorkspaceSuggestionOutputHygiene.Apply(narrativeField.Kind, narrativeField.Text)),
                cancellationToken);
        }

        return true;
    }
}
