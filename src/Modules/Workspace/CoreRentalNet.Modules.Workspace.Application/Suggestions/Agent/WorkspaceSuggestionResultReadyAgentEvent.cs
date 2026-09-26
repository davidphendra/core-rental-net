namespace CoreRentalNet.Modules.Workspace.Application.Suggestions.Agent;

/// <summary>The terminal answer, already mapped into the application's vocabulary, and the hash it was drawn from.</summary>
public sealed record WorkspaceSuggestionResultReadyAgentEvent(
    WorkspaceSuggestionAnswer SuggestionAnswer,
    string PayloadHash) : WorkspaceSuggestionAgentEvent;
