namespace CoreRentalNet.Host.Components.Shared.Suggestion;

/// <summary>The application's own stage lines, collapsible once a run has ended.</summary>
public sealed record SuggestionStageNotice(IReadOnlyList<string> StageWords, bool Collapsed) : SuggestionNotice;
