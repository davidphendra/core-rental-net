namespace CoreRentalNet.Modules.Workspace.Application.Suggestions.Agent.Events;

/// <summary>A fragment of the agent's own text, exactly as it arrived.</summary>
public sealed record WorkspaceSuggestionNarrativeDeltaEvent(string NarrativeText) : WorkspaceSuggestionEvent;
