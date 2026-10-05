namespace CoreRentalNet.Modules.Workspace.Application.Suggestions.Agent.Events;

/// <summary>That the agent has finished a processing stage, named in the agent's own vocabulary.</summary>
/// <remarks>
/// The pair to <see cref="WorkspaceSuggestionStageStartedEvent"/>: begun and finished are two facts, because the
/// panel marks a node done when this arrives, and a run can end with a node begun and never finished.
/// </remarks>
public sealed record WorkspaceSuggestionStageCompletedEvent(string AgentProcessingStage)
    : WorkspaceSuggestionEvent;
