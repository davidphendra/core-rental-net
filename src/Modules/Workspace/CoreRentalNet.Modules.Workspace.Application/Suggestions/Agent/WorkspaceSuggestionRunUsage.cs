namespace CoreRentalNet.Modules.Workspace.Application.Suggestions.Agent;

/// <summary>What the run cost, as the agent reported it.</summary>
/// <remarks>
/// Recorded on the run rather than guessed at, and the model call count is the field that makes the "two
/// calls" assumption visible if it ever becomes three. The provider's own report shape stays in
/// infrastructure; the adapter maps it onto this.
/// </remarks>
public sealed record WorkspaceSuggestionRunUsage(
    int ModelCalls,
    int InputTokens,
    int OutputTokens,
    string Model,
    string PromptVersion);
