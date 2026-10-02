namespace CoreRentalNet.Modules.Workspace.Application.Suggestions.Run.Events;

/// <summary>One thing a run writes to the browser, in the application's own vocabulary.</summary>
/// <remarks>
/// A closed hierarchy, so the writer's <c>switch</c> over it is exhaustive: a new frame is a new type and a
/// compile error at the writer rather than a frame that silently goes nowhere.
/// </remarks>
public abstract record WorkspaceSuggestionStreamEvent
{
    private protected WorkspaceSuggestionStreamEvent()
    {
    }
}
