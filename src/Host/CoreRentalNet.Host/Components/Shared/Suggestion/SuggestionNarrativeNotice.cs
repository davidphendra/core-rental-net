namespace CoreRentalNet.Host.Components.Shared.Suggestion;

/// <summary>The model's own words, a finished field at a time, as prose.</summary>
public sealed record SuggestionNarrativeNotice(IReadOnlyList<string> NarrativeLines) : SuggestionNotice;
