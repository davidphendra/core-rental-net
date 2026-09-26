namespace CoreRentalNet.Modules.Workspace.Application.Suggestions.Run;

/// <summary>Where a run writes what happens while it happens.</summary>
/// <remarks>
/// A port, so a run is testable with a recording fake and never sees an HTTP response. The framing - the event
/// name and the blank line - belongs to the adapter in the composition root.
/// </remarks>
public interface IWorkspaceSuggestionEventWriter
{
    Task WriteAsync(WorkspaceSuggestionStreamEvent suggestionStreamEvent, CancellationToken cancellationToken);
}
