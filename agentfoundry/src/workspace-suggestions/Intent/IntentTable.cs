using System.Text.Json;
using AgentFoundry.WorkspaceSuggestions.Vocabularies;

namespace AgentFoundry.WorkspaceSuggestions.Intent;

/// <summary>
/// The declared phrasings that mean a slot set, and nothing else.
/// </summary>
/// <remarks>
/// <para>
/// The lookup is code, not a model's decision. That is what makes a hit reproducible: the same request
/// produces the same slot set every time, and a test can assert it. A phrase the table does not hold
/// falls through to <see cref="ISlotClassifier"/>, whose answer is marked as inferred - so the table
/// being incomplete is visible in the result rather than silently filled in by a model.
/// </para>
/// <para>
/// The table is a JSON file rather than a literal so that changing the policy is a reviewable diff in
/// a data file, and so a phrase can be added without a rebuild. It is validated on load: a slot
/// outside the closed vocabulary, a blank phrase or an empty row is refused by name rather than
/// producing a specification nobody can check.
/// </para>
/// </remarks>
public sealed class IntentTable
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
        AllowTrailingCommas = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
    };

    private readonly IReadOnlyList<IntentRule> _rules;

    private IntentTable(IReadOnlyList<IntentRule> rules) => _rules = rules;

    /// <summary>How many phrasings the table declares. Asserted, so a table that loaded nothing fails loudly.</summary>
    public int Count => _rules.Count;

    /// <summary>Reads and validates the table, or says which row is wrong.</summary>
    public static IntentTable Load(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new IntentTableException("No intent table path was configured.");
        }

        if (!File.Exists(path))
        {
            throw new IntentTableException($"Intent table not found: '{path}'.");
        }

        List<IntentRow>? rows;

        try
        {
            rows = JsonSerializer.Deserialize<List<IntentRow>>(File.ReadAllText(path), Options);
        }
        catch (JsonException exception)
        {
            throw new IntentTableException($"Intent table is not valid JSON: '{path}'. {exception.Message}", exception);
        }

        if (rows is not { Count: > 0 })
        {
            throw new IntentTableException($"Intent table holds no phrasings: '{path}'.");
        }

        return new IntentTable([.. rows.Select(row => Rule(row, path))]);
    }

    /// <summary>The slot set a request means, or null when the table does not know the phrasing.</summary>
    /// <remarks>
    /// The longest declared phrase that appears in the request wins, so a specific phrasing beats a
    /// general one without the file having to be ordered. Words are compared whole: "desk" must not be
    /// found inside "deskpad", which is the kind of match that looks right until someone types it.
    /// </remarks>
    public IReadOnlyList<string>? Match(string? query)
    {
        var normalised = QueryText.Normalise(query);

        if (normalised.Length == 0)
        {
            return null;
        }

        var padded = $" {normalised} ";

        return _rules
            .SelectMany(rule => rule.Phrases.Select(phrase => (Phrase: phrase, rule.Slots)))
            .Where(candidate => padded.Contains($" {candidate.Phrase} ", StringComparison.Ordinal))
            .OrderByDescending(candidate => candidate.Phrase.Length)
            .Select(candidate => candidate.Slots)
            .FirstOrDefault();
    }

    private static IntentRule Rule(IntentRow row, string path)
    {
        var phrases = (row.Phrases ?? [])
            .Select(QueryText.Normalise)
            .Where(phrase => phrase.Length > 0)
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        if (phrases.Length == 0)
        {
            throw new IntentTableException($"Intent table has a row with no usable phrase: '{path}'.");
        }

        var slots = row.Slots ?? [];

        if (slots.Count == 0)
        {
            throw new IntentTableException(
                $"Intent table row '{phrases[0]}' declares no slots: '{path}'.");
        }

        foreach (var slot in slots)
        {
            if (!Slots.IsKnown(slot))
            {
                throw new IntentTableException(
                    $"Intent table row '{phrases[0]}' names an unknown slot '{slot}': '{path}'.");
            }
        }

        return new IntentRule(phrases, slots);
    }
}
