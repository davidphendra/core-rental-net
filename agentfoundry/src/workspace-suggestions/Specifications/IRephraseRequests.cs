using AgentFoundry.WorkspaceSuggestions.Contracts;

namespace AgentFoundry.WorkspaceSuggestions.Specifications;

/// <summary>
/// Turns a request into the specification the rest of the run works from.
/// </summary>
/// <remarks>
/// A port because it is the workflow's second node and the application-facing side of it must be
/// testable without a model. Its implementation is the only author of the specification: the reviewer
/// sends findings back here rather than writing one, so the slot vocabulary and the capacity bounds
/// are enforced in one place.
/// </remarks>
public interface IRephraseRequests
{
    /// <param name="request">What the customer asked for, and the slot rules the application owns.</param>
    /// <param name="findings">What a reviewer objected to last time, empty on the first attempt.</param>
    Task<Specification> RephraseAsync(
        SuggestionRequest request,
        IReadOnlyList<Finding> findings,
        CancellationToken cancellationToken = default);
}
