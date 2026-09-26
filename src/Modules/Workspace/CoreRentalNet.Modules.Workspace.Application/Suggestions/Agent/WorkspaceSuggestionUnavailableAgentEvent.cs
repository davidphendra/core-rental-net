namespace CoreRentalNet.Modules.Workspace.Application.Suggestions.Agent;

/// <summary>The run could not be made: no agent, no identity, no transport, no parse, or too slow.</summary>
public sealed record WorkspaceSuggestionUnavailableAgentEvent(string Reason) : WorkspaceSuggestionAgentEvent;
