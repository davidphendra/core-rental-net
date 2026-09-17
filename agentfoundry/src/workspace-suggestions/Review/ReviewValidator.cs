using AgentFoundry.WorkspaceSuggestions.Contracts;
using AgentFoundry.WorkspaceSuggestions.Selection;
using AgentFoundry.WorkspaceSuggestions.Specifications;

namespace AgentFoundry.WorkspaceSuggestions.Review;

/// <summary>
/// The checks that are computable, made in code rather than asked of a model.
/// </summary>
/// <remarks>
/// <para>
/// Every invariant here is one a language model cannot see and should not be asked to guess at. The
/// reviewer holds no catalogue, so what it can check is the composition against the receipt the
/// suggestor sent - which is exactly the set of things that are wrong without looking wrong.
/// </para>
/// <para>
/// A failure here is repaired by re-composing, not by asking the rephraser for different words: the
/// words were not the problem. That distinction is the whole reason this is code and the semantic
/// check is a model.
/// </para>
/// </remarks>
internal static class ReviewValidator
{
    public static IReadOnlyList<Finding> Validate(
        Specification specification,
        IReadOnlyList<SuggestionOption> options,
        IReadOnlyDictionary<string, IReadOnlyList<string>> ordered)
    {
        ArgumentNullException.ThrowIfNull(specification);
        ArgumentNullException.ThrowIfNull(options);

        var findings = new List<Finding>();

        if (options.Count == 0)
        {
            findings.Add(new Finding("tier_composition", FirstSlot(specification)));

            return findings;
        }

        foreach (var option in options)
        {
            findings.AddRange(Slots(option, specification));
            findings.AddRange(Tiers(option, ordered));
        }

        return findings;
    }

    /// <summary>Every slot the specification names is filled, once, with the quantity it asked for.</summary>
    private static IEnumerable<Finding> Slots(SuggestionOption option, Specification specification)
    {
        foreach (var requirement in specification.Slots)
        {
            var lines = option.Lines.Where(line => line.Slot == requirement.Slot).ToArray();

            if (lines.Length != 1 || lines[0].Quantity != requirement.Quantity)
            {
                yield return new Finding("tier_composition", requirement.Slot);
            }
        }

        foreach (var line in option.Lines.Where(line => specification.Slots.All(slot => slot.Slot != line.Slot)))
        {
            yield return new Finding("tier_composition", line.Slot);
        }
    }

    /// <summary>
    /// The product chosen for each slot is the one at that tier's position.
    /// </summary>
    /// <remarks>
    /// This is the check that needs the receipt: re-deriving the pick from the ordered candidates is
    /// what catches a picker that returned the wrong position, which would change what a customer is
    /// offered while every other check still passed.
    /// </remarks>
    private static IEnumerable<Finding> Tiers(
        SuggestionOption option,
        IReadOnlyDictionary<string, IReadOnlyList<string>> ordered)
    {
        var position = Tier.PositionOf(option.Tier);

        if (position < 0)
        {
            yield break;
        }

        foreach (var line in option.Lines)
        {
            if (!ordered.TryGetValue(line.Slot, out var candidates) || candidates.Count == 0)
            {
                yield return new Finding("tier_composition", line.Slot);

                continue;
            }

            var expected = candidates[Pick(candidates.Count, position)];

            if (!string.Equals(expected, line.Sku, StringComparison.OrdinalIgnoreCase))
            {
                yield return new Finding("tier_composition", line.Slot);
            }
        }
    }

    /// <summary>The position a tier reads from, matching the picker: cheapest, middle index, dearest.</summary>
    private static int Pick(int count, int position)
        => position switch
        {
            0 => 0,
            _ when position >= Tier.All.Count - 1 => count - 1,
            _ => count / 2,
        };

    private static string FirstSlot(Specification specification)
        => specification.Slots.Count > 0 ? specification.Slots[0].Slot : Vocabularies.Slots.Desk;
}
