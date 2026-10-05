namespace CoreRentalNet.Host.Components.Shared.Suggestion;

/// <summary>A run that could not be completed.</summary>
public sealed record SuggestionFailureNotice(string FailureCode) : SuggestionNotice;
