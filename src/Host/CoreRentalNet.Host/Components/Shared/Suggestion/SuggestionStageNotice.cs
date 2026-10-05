namespace CoreRentalNet.Host.Components.Shared.Suggestion;

/// <summary>The application's own timeline lines, collapsible once a run has ended.</summary>
/// <remarks>
/// <paramref name="Collapsed"/> is whether the list is folded away, and <paramref name="IsRunning"/> is whether
/// the run is still going. The view needs both: while a run goes the list is always shown because it is the
/// progress, and a line still active after the run has stopped is not in progress and must not spin.
/// </remarks>
public sealed record SuggestionStageNotice(IReadOnlyList<SuggestionStageLine> Lines, bool Collapsed, bool IsRunning)
    : SuggestionNotice;
