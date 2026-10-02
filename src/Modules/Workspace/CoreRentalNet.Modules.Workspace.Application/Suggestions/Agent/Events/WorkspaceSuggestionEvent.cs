namespace CoreRentalNet.Modules.Workspace.Application.Suggestions.Agent.Events;

/// <summary>One thing that happened during a run, in order, in the application's own words.</summary>
/// <remarks>
/// A closed hierarchy: the three things a caller can be told are the three types beside it, so the stream
/// processor's <c>switch</c> over them is exhaustive and a fourth kind cannot appear without a compile error
/// at every call site. The application's run service is the only consumer.
/// </remarks>
public abstract record WorkspaceSuggestionEvent
{
    private protected WorkspaceSuggestionEvent()
    {
    }
}
