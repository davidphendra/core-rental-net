namespace CoreRentalNet.Host.Components.Shared.Suggestion;

/// <summary>One line of the run's timeline: a stage, a retry or a found setup, and whether it is done.</summary>
/// <remarks>
/// The panel's own model. A stage begins active and is completed by the <c>stageCompleted</c> frame; a retry and a
/// found setup are facts that are over the moment they arrive, so they are born complete.
/// </remarks>
public sealed record SuggestionStageLine(string Words, bool IsComplete);
