using AgentFoundry.WorkspaceSuggestions.Contracts;

namespace AgentFoundry.WorkspaceSuggestions.Intent;

/// <summary>
/// Answers "which slots does this request mean" when the table does not know the phrasing.
/// </summary>
/// <remarks>
/// A port, because that answer is the one thing a model is for here and the unit tier must run without
/// one. What it returns is checked against the closed slot vocabulary before it is used: the model
/// cannot invent a slot, and an answer outside the list is a failure rather than a suggestion.
/// </remarks>
public interface ISlotClassifier
{
    /// <param name="query">The customer's own words.</param>
    /// <param name="slots">The slots the application says exist, which is what the answer is checked against.</param>
    /// <param name="findings">What a reviewer objected to on a previous attempt, which is why this one is being asked again.</param>
    Task<IReadOnlyList<string>> ClassifyAsync(
        string query,
        IReadOnlyList<SlotRule> slots,
        IReadOnlyList<Finding> findings,
        CancellationToken cancellationToken = default);
}
