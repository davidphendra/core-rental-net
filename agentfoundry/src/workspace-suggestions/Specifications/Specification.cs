using AgentFoundry.WorkspaceSuggestions.Contracts;

namespace AgentFoundry.WorkspaceSuggestions.Specifications;

/// <summary>
/// What the request means: which slots the workspace has, and how many units each holds.
/// </summary>
/// <remarks>
/// <para>
/// Authored by one component, and only one. The reviewer emits findings and never rewrites this, so the
/// slot vocabulary, the capacity bounds and the inferred marking are enforced in exactly one place
/// rather than in two that can disagree.
/// </para>
/// <para>
/// The slot set is fixed here and identical across every candidate: the three options differ in what
/// fills each slot, not in which slots exist, which is what makes them comparable and what the
/// reviewer's checks are written against.
/// </para>
/// <para>
/// The request it was built from travels with it. A retry has to produce a specification from the same
/// rules - the capacities are the application's - and a specification that could not say what it was
/// derived from would have to be handed the request again by every node between here and the reviewer.
/// </para>
/// </remarks>
public sealed record Specification(SuggestionRequest Request, IReadOnlyList<SlotRequirement> Slots)
{
    /// <summary>Ties this specification to the run that produced it.</summary>
    public string RequestId => Request.RequestId;

    /// <summary>The customer's own words, which the criteria are matched from. Never rewritten.</summary>
    public string Query => Request.Query;

    /// <summary>True when any slot came from the model rather than the table.</summary>
    public bool HasInferredSlots => Slots.Any(slot => slot.Inferred);
}
