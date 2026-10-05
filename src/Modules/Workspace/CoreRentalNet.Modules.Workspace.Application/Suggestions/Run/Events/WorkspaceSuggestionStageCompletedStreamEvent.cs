namespace CoreRentalNet.Modules.Workspace.Application.Suggestions.Run.Events;

/// <summary>One app-owned stage line finishing, in the customer's words.</summary>
public sealed record WorkspaceSuggestionStageCompletedStreamEvent(string StageWords)
    : WorkspaceSuggestionStreamEventBase;
