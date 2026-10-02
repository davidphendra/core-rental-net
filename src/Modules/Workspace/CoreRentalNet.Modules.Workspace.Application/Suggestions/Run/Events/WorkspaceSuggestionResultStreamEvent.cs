using CoreRentalNet.Modules.Workspace.Application.Suggestions.Agent;

namespace CoreRentalNet.Modules.Workspace.Application.Suggestions.Run.Events;

/// <summary>The terminal answer, whole, as one frame.</summary>
public sealed record WorkspaceSuggestionResultStreamEvent(WorkspaceSuggestionResultFrame ResultFrame)
    : WorkspaceSuggestionStreamEvent;
