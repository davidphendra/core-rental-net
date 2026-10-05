namespace CoreRentalNet.Host.Components.Shared.Suggestion;

/// <summary>The application's own timeline lines, collapsible once a run has ended.</summary>
public sealed record SuggestionStageNotice(IReadOnlyList<SuggestionStageLine> Lines, bool Collapsed) : SuggestionNotice;
