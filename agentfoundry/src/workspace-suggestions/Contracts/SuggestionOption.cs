namespace AgentFoundry.WorkspaceSuggestions.Contracts;

/// <summary>One candidate workspace, at one budget tier.</summary>
/// <remarks>
/// The tier is what the option means: every slot in it was chosen at the same price position, so a
/// caller that merges two options produces a workspace that is none of the three, and the label stops
/// being true. <c>pinnedSlots</c> says where the tier means nothing because the slot had too few
/// candidates to differ.
/// </remarks>
public sealed record SuggestionOption(
    string Tier,
    IReadOnlyList<OptionLine> Lines,
    IReadOnlyList<string> Criteria,
    IReadOnlyList<UnevaluatedCriterion> Unevaluated,
    IReadOnlyList<string> PinnedSlots);
