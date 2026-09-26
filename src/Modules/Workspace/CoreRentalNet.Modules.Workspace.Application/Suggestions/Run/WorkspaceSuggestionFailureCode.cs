namespace CoreRentalNet.Modules.Workspace.Application.Suggestions.Run;

/// <summary>The codes a run reports when there is no answer, in one place.</summary>
/// <remarks>
/// A code rather than a sentence, and a stable one: the browser owns the words, so the difference between a
/// failure and a refusal is decided in one place. The reason the agent gave is diagnostic and does not travel
/// here.
/// </remarks>
public static class WorkspaceSuggestionFailureCode
{
    /// <summary>The code a run that could not be made reports: no agent, no identity, or no transport.</summary>
    public const string Unavailable = "unavailable";

    /// <summary>The answer was not one this application can honour, so there is nothing to show.</summary>
    public const string Invalid = "invalid";
}
