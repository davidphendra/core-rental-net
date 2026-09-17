using AgentFoundry.WorkspaceSuggestions.Contracts;

namespace AgentFoundry.WorkspaceSuggestions.Review;

/// <summary>
/// Judges whether a composition still serves the request, and reports what is wrong when it does not.
/// </summary>
/// <remarks>
/// The one judgement in the run that is not computable, which is why it is the one a model makes. It
/// returns findings and never an option: a component that both judged and rewrote would be reviewing
/// its own work, and the specification would then have two authors.
/// </remarks>
public interface IReviewComposition
{
    Task<IReadOnlyList<Finding>> ReviewAsync(
        string query,
        IReadOnlyList<SuggestionOption> options,
        CancellationToken cancellationToken = default);
}
