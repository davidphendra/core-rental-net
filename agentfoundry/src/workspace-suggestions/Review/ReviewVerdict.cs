using AgentFoundry.WorkspaceSuggestions.Contracts;

namespace AgentFoundry.WorkspaceSuggestions.Review;

/// <summary>What the reviewer decided: whether the composition holds up, and what is wrong if it does not.</summary>
/// <param name="Approved">True when nothing was found to object to.</param>
/// <param name="Findings">What was found, structured so the application renders it rather than quoting it.</param>
/// <param name="Repeated">
/// True when this attempt produced the same specification as the last one, so asking again cannot help.
/// The attempt budget exists to bound the cost of trying, not to spend it on a loop that is not moving.
/// </param>
public sealed record ReviewVerdict(bool Approved, IReadOnlyList<Finding> Findings, bool Repeated);
