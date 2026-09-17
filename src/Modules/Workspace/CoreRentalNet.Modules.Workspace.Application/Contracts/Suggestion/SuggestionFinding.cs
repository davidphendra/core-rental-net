namespace CoreRentalNet.Modules.Workspace.Application.Contracts.Suggestion;

/// <summary>An objection the application will render, in the application's own terms.</summary>
/// <remarks>
/// Distinct from <see cref="AgentFinding"/> for the same reason an option is distinct from the agent's
/// option: the wire's shape is the wire's, and what the page renders is the application's. The two
/// happen to look alike today, and a change to either should not silently become a change to both.
/// </remarks>
public sealed record SuggestionFinding(string Kind, string Slot);
