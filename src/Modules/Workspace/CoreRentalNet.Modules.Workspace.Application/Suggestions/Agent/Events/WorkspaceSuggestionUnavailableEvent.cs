namespace CoreRentalNet.Modules.Workspace.Application.Suggestions.Agent.Events;

/// <summary>The run could not be made: no agent, no identity, no transport, no parse, or too slow.</summary>
public sealed record WorkspaceSuggestionUnavailableEvent(string Reason) : WorkspaceSuggestionEvent;
