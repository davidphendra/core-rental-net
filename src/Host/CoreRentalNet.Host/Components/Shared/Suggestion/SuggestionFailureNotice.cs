namespace CoreRentalNet.Host.Components.Shared.Suggestion;

/// <summary>A run that could not be completed, and whether streamed text is on screen with nothing applied.</summary>
public sealed record SuggestionFailureNotice(string FailureCode, bool NotApplied) : SuggestionNotice;
