using AgentFoundry.WorkspaceSuggestions.Contracts;
using AgentFoundry.WorkspaceSuggestions.Specifications;

namespace AgentFoundry.WorkspaceSuggestions.Review;

/// <summary>The run's gate: everything that can be checked, checked before anything is returned.</summary>
public interface IReviewCandidates
{
    Task<ReviewVerdict> ReviewAsync(
        Specification specification,
        IReadOnlyList<SuggestionOption> options,
        IReadOnlyDictionary<string, IReadOnlyList<string>> ordered,
        CancellationToken cancellationToken = default);
}
