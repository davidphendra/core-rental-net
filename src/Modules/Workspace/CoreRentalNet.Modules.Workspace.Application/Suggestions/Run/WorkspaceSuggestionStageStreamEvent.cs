namespace CoreRentalNet.Modules.Workspace.Application.Suggestions.Run;

/// <summary>One app-owned stage line, in the customer's words.</summary>
public sealed record WorkspaceSuggestionStageStreamEvent(string StageWords) : WorkspaceSuggestionStreamEvent;
