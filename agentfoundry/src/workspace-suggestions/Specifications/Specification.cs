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
/// </remarks>
public sealed record Specification(string RequestId, IReadOnlyList<SlotRequirement> Slots)
{
    /// <summary>True when any slot came from the model rather than the table.</summary>
    public bool HasInferredSlots => Slots.Any(slot => slot.Inferred);
}
