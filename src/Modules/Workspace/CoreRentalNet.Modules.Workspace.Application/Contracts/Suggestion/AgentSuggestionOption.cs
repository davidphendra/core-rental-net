namespace CoreRentalNet.Modules.Workspace.Application.Contracts.Suggestion;

/// <summary>One candidate the agent composed, before the application has checked any of it.</summary>
/// <remarks>
/// Its SKUs are unchecked and its criteria are tokens the application renders. It is deliberately raw:
/// what the agent said and what the application will show are two different things, and keeping them
/// apart is what makes the checking in between visible.
/// </remarks>
public sealed record AgentSuggestionOption(
    string Tier,
    IReadOnlyList<AgentSuggestionLine> Lines,
    IReadOnlyList<string> Criteria,
    IReadOnlyList<string> Unevaluated,
    IReadOnlyList<string> PinnedSlots);
