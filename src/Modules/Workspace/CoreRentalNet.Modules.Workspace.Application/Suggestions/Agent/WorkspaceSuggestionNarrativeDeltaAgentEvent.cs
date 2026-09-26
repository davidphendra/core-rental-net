namespace CoreRentalNet.Modules.Workspace.Application.Suggestions.Agent;

/// <summary>A fragment of the agent's own text, exactly as it arrived.</summary>
public sealed record WorkspaceSuggestionNarrativeDeltaAgentEvent(string NarrativeText) : WorkspaceSuggestionAgentEvent;
