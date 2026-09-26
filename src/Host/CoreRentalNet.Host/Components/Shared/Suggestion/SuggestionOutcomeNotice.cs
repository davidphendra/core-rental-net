using CoreRentalNet.Modules.Workspace.Application.Suggestions.Agent;

namespace CoreRentalNet.Host.Components.Shared.Suggestion;

/// <summary>The terminal answer: the candidates, or the typed not-a-workspace refusal.</summary>
public sealed record SuggestionOutcomeNotice(WorkspaceSuggestionResultFrame ResultFrame) : SuggestionNotice;
