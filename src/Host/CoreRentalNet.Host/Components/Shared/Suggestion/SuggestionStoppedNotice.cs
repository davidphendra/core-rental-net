namespace CoreRentalNet.Host.Components.Shared.Suggestion;

/// <summary>A run the customer stopped, and whether streamed text is on screen with nothing applied.</summary>
public sealed record SuggestionStoppedNotice(bool NotApplied) : SuggestionNotice;
