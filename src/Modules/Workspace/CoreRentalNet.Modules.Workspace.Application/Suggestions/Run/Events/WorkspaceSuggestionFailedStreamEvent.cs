namespace CoreRentalNet.Modules.Workspace.Application.Suggestions.Run.Events;

/// <summary>That the run could not be made or could not be honoured, with the code the browser words.</summary>
public sealed record WorkspaceSuggestionFailedStreamEvent(string FailureCode) : WorkspaceSuggestionStreamEvent;
