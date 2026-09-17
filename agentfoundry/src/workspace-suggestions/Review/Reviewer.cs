using AgentFoundry.WorkspaceSuggestions.Contracts;
using AgentFoundry.WorkspaceSuggestions.Specifications;

namespace AgentFoundry.WorkspaceSuggestions.Review;

/// <summary>
/// The gate: computable checks first, then one judgement, and nothing returned that failed either.
/// </summary>
/// <remarks>
/// <para>
/// The order matters. A composition that breaks an invariant is refused in code without asking a model
/// anything, because the model cannot see the invariants and asking it would spend a call to learn
/// nothing. Only a composition that is structurally sound reaches the judgement, and only that
/// judgement can send work back to be rephrased.
/// </para>
/// <para>
/// The specification of the previous attempt is remembered here, because this is where asking again is
/// decided. A retry that produces the same specification as the last one is not a second attempt, it is
/// the same one twice, and the budget should not be spent on it.
/// </para>
/// </remarks>
public sealed class Reviewer(IReviewComposition judgement) : IReviewCandidates
{
    private string? _previous;

    public async Task<ReviewVerdict> ReviewAsync(
        Specification specification,
        IReadOnlyList<SuggestionOption> options,
        IReadOnlyDictionary<string, IReadOnlyList<string>> ordered,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(specification);
        ArgumentNullException.ThrowIfNull(options);

        var repeated = _previous is not null && _previous == Fingerprint(specification);
        _previous = Fingerprint(specification);

        var structural = ReviewValidator.Validate(specification, options, ordered);

        if (structural.Count > 0)
        {
            return new ReviewVerdict(Approved: false, structural, repeated);
        }

        var judged = await judgement.ReviewAsync(specification.Query, options, cancellationToken);

        return new ReviewVerdict(judged.Count == 0, judged, repeated);
    }

    /// <summary>The parts of a specification a retry could change, and only those.</summary>
    private static string Fingerprint(Specification specification)
        => string.Join('|', specification.Slots.Select(slot => $"{slot.Slot}:{slot.Quantity}:{slot.Inferred}"));
}
