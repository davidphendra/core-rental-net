using CoreRentalNet.Modules.Workspace.Application.Suggestions.Run;

namespace CoreRentalNet.Modules.Workspace.UnitTests.Suggestions;

/// <summary>A hand-written outbound port. No mocking library is used anywhere in this project.</summary>
internal sealed class RecordingWorkspaceSuggestionEventWriter : IWorkspaceSuggestionEventWriter
{
    public List<WorkspaceSuggestionStreamEvent> Events { get; } = [];

    public Task WriteAsync(WorkspaceSuggestionStreamEvent suggestionStreamEvent, CancellationToken cancellationToken)
    {
        Events.Add(suggestionStreamEvent);

        return Task.CompletedTask;
    }
}
