namespace CoreRentalNet.Modules.Workspace.Application.Suggestions.Agent;

/// <summary>That the agent has begun a processing stage, named in the agent's own vocabulary.</summary>
/// <remarks>
/// The agent names the stage and the application words it: the two are different languages, and keeping the
/// agent's name out of the panel is what lets the copy change without changing the workflow.
/// </remarks>
public sealed record WorkspaceSuggestionStageChangedAgentEvent(string AgentProcessingStage)
    : WorkspaceSuggestionAgentEvent;
