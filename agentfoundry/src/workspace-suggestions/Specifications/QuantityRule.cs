using AgentFoundry.WorkspaceSuggestions.Contracts;
using AgentFoundry.WorkspaceSuggestions.Intent;

namespace AgentFoundry.WorkspaceSuggestions.Specifications;

/// <summary>How many units of a slot a request asks for.</summary>
/// <remarks>
/// A count written in the request is honoured, and everything else is one. The result is clamped to the
/// capacity the request carried, because the application owns that number: a suggestion for four
/// monitors when the canvas holds three is one the customer cannot accept, and the clamp is what keeps
/// a suggestion applicable rather than merely plausible.
/// </remarks>
internal static class QuantityRule
{
    private static readonly Dictionary<string, int> Counted = new(StringComparer.Ordinal)
    {
        ["one"] = 1,
        ["two"] = 2,
        ["three"] = 3,
        ["four"] = 4,
        ["five"] = 5,
    };

    public static int For(string? query, SlotRule slot)
    {
        ArgumentNullException.ThrowIfNull(slot);

        var asked = Asked(QueryText.Normalise(query), QueryText.Normalise(slot.DisplayName));

        return Math.Clamp(asked, 1, slot.MaxQuantity);
    }

    private static int Asked(string normalised, string slotWords)
    {
        var words = normalised.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var slot = slotWords.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        if (slot.Length == 0)
        {
            return 1;
        }

        for (var index = 0; index < words.Length; index++)
        {
            // "two monitors" and "monitors two" are both ways of saying it, and neither is worth
            // choosing between: a request in the wild uses whichever comes out first.
            if (Number(words[index]) is { } before && StartsAt(words, index + 1, slot))
            {
                return before;
            }

            if (index > 0 && StartsAt(words, index, slot) && Number(words[index - 1]) is { } after)
            {
                return after;
            }
        }

        return 1;
    }

    private static bool StartsAt(string[] words, int index, string[] slot)
        => index >= 0
            && index + slot.Length <= words.Length
            && words.Skip(index).Take(slot.Length).SequenceEqual(slot, StringComparer.Ordinal);

    private static int? Number(string word)
        => Counted.TryGetValue(word, out var written)
            ? written
            : int.TryParse(word, out var digits) ? digits : null;
}
