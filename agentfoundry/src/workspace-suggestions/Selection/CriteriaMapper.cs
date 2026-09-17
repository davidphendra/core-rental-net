using AgentFoundry.WorkspaceSuggestions.Catalogue;
using AgentFoundry.WorkspaceSuggestions.Contracts;
using AgentFoundry.WorkspaceSuggestions.Intent;

namespace AgentFoundry.WorkspaceSuggestions.Selection;

/// <summary>What a candidate satisfies, and what the catalogue cannot express.</summary>
/// <remarks>
/// Both halves travel together because the second is the point: a criterion that matched nothing is
/// reported rather than dropped, so a customer is told what the catalogue cannot check instead of being
/// shown an option that silently ignored half of what they asked for.
/// </remarks>
internal sealed record CriteriaMap(
    IReadOnlyList<string> Matched,
    IReadOnlyList<UnevaluatedCriterion> Unevaluated);

/// <summary>
/// Turns the words of a request into tokens the catalogue can be checked against, or reports them.
/// </summary>
/// <remarks>
/// <para>
/// A criterion is a token rather than a sentence - <c>slot:Monitor</c>, <c>quantity:Monitor:2</c>,
/// <c>tag:standing</c>, <c>attribute:Monitor:panel:IPS</c> - because the application renders the copy
/// and the model never writes it. Matching is against the products actually chosen for this candidate,
/// so one option can satisfy a word that another does not, which is the honest answer.
/// </para>
/// <para>
/// An attribute is preferred to a tag when both match, because it says more: <c>panel:IPS</c> is a fact
/// about the product where <c>tag:ips</c> is a label somebody attached.
/// </para>
/// </remarks>
internal static class CriteriaMapper
{
    /// <summary>Grammar, not meaning: these are not criteria for anything, so they are not reported.</summary>
    private static readonly HashSet<string> Grammar = new(StringComparer.Ordinal)
    {
        "a", "an", "the", "for", "and", "or", "with", "of", "to", "in", "on", "at", "from",
        "my", "me", "i", "we", "you", "it", "that", "this",
        "is", "are", "be", "want", "would", "like", "need", "have", "has", "can", "could",
        "should", "will", "please", "some", "something", "setup", "set",

        // Counts are read by the quantity rule and reported as `quantity:...`, so they are not also a
        // criterion the catalogue failed to express - reporting them would tell a customer that "two"
        // could not be checked.
        "one", "two", "three", "four", "five", "six", "1", "2", "3", "4", "5", "6",
    };

    public static CriteriaMap For(
        string query,
        IReadOnlyList<OptionLine> lines,
        IReadOnlyDictionary<string, CatalogueItem> chosen)
    {
        ArgumentNullException.ThrowIfNull(lines);

        var matched = new List<string>();
        var unevaluated = new List<UnevaluatedCriterion>();

        foreach (var line in lines)
        {
            matched.Add($"slot:{line.Slot}");

            if (line.Quantity > 1)
            {
                matched.Add($"quantity:{line.Slot}:{line.Quantity}");
            }
        }

        var slotWords = chosen.Values
            .Select(item => item.SubCategory ?? item.Category)
            .Where(word => !string.IsNullOrWhiteSpace(word))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var word in Words(query))
        {
            if (slotWords.Contains(word))
            {
                // Said by the slot itself, so it is already satisfied and must not also be reported.
                continue;
            }

            var attribute = Attribute(word, chosen);

            if (attribute is not null)
            {
                matched.Add(attribute);
            }
            else if (chosen.Values.Any(item => item.Metadata.Tags.Contains(word, StringComparer.OrdinalIgnoreCase)))
            {
                matched.Add($"tag:{word}");
            }
            else
            {
                unevaluated.Add(new UnevaluatedCriterion(word, "not_in_catalogue"));
            }
        }

        return new CriteriaMap(matched, unevaluated);
    }

    /// <summary>The first attribute whose value is this word, as the token the contract names.</summary>
    private static string? Attribute(string word, IReadOnlyDictionary<string, CatalogueItem> chosen)
    {
        foreach (var (slot, item) in chosen)
        {
            foreach (var (key, value) in item.Metadata.Attributes)
            {
                if (string.Equals(QueryText.Normalise(value), word, StringComparison.Ordinal))
                {
                    return $"attribute:{slot}:{key}:{value}";
                }
            }
        }

        return null;
    }

    private static IEnumerable<string> Words(string? query)
        => QueryText
            .Normalise(query)
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Where(word => !Grammar.Contains(word))
            .Distinct(StringComparer.Ordinal);
}
