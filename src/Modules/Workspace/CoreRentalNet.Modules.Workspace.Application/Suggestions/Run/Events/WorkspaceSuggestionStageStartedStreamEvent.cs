namespace CoreRentalNet.Modules.Workspace.Application.Suggestions.Run.Events;

/// <summary>One app-owned stage line, in the customer's words.</summary>
public sealed record WorkspaceSuggestionStageStartedStreamEvent(string StageWords) : WorkspaceSuggestionStreamEventBase;
