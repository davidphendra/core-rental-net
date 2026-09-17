namespace AgentFoundry.WorkspaceSuggestions.Selection;

/// <summary>The three budget tiers a workspace is offered at, named once.</summary>
/// <remarks>
/// A tier is a position in a slot's candidates ordered by price, not a price: every slot in one option
/// is filled at the same position, which is what makes the option mean something as a whole. A caller
/// that merges two options produces a workspace that is none of the three, and the label stops being
/// true - which is why the application replaces rather than merges.
/// </remarks>
public static class Tier
{
    public const string Low = "low";
    public const string Middle = "middle";
    public const string High = "high";

    /// <summary>Every tier, in the order they are offered and in the order of their positions.</summary>
    public static readonly IReadOnlyList<string> All = [Low, Middle, High];

    /// <summary>Where a tier sits among the positions, or -1 when the text names no tier.</summary>
    public static int PositionOf(string? tier)
    {
        for (var index = 0; index < All.Count; index++)
        {
            if (string.Equals(All[index], tier, StringComparison.Ordinal))
            {
                return index;
            }
        }

        return -1;
    }
}
