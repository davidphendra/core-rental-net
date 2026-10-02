namespace CoreRentalNet.Modules.Workspace.Application.Suggestions.Run.Events;

/// <summary>One completed narrative field, already hygiened, in the model's words.</summary>
public sealed record WorkspaceSuggestionTextStreamEvent(string NarrativeWords) : WorkspaceSuggestionStreamEvent;
