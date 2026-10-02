namespace CoreRentalNet.Modules.Workspace.Application.Suggestions.Agent.Events;

/// <summary>The terminal answer, already mapped into the application's vocabulary, and the hash it was drawn from.</summary>
public sealed record WorkspaceSuggestionResultReadyEvent(
    WorkspaceSuggestionAnswer SuggestionAnswer,
    string PayloadHash) : WorkspaceSuggestionEvent;
