namespace CoreRentalNet.Modules.Workspace.Application.Suggestions.Run.Events;

/// <summary>One attempt beginning again, in the customer's words.</summary>
public sealed record WorkspaceSuggestionRetryStreamEvent(string RetryWords) : WorkspaceSuggestionStreamEvent;
