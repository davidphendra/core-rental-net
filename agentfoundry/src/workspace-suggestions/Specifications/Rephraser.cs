using AgentFoundry.WorkspaceSuggestions.Contracts;
using AgentFoundry.WorkspaceSuggestions.Intent;
using Microsoft.Extensions.Logging;

namespace AgentFoundry.WorkspaceSuggestions.Specifications;

/// <summary>
/// The specification's only author: a declared table first, and a model only when the table misses.
/// </summary>
/// <remarks>
/// <para>
/// The table lookup is code rather than a model's decision, so a known phrasing produces the same slot
/// set every time and a test can assert it. A miss is marked <c>Inferred</c> and logged, so the model's
/// answer is never mistaken for a declared one and an incomplete table is visible as a rate.
/// </para>
/// <para>
/// Findings from a previous attempt are passed to the model rather than used here: a table cannot
/// reason about why an option was rejected, and pretending otherwise would be a rule that never fires.
/// </para>
/// </remarks>
public sealed class Rephraser(IntentTable table, ISlotClassifier classifier, ILogger<Rephraser> log)
    : IRephraseRequests
{
    public async Task<Specification> RephraseAsync(
        SuggestionRequest request,
        IReadOnlyList<Finding> findings,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(findings);

        if (table.Match(request.Query) is { } declared)
        {
            return new Specification(request, Requirements(request, declared, inferred: false));
        }

        IntentLog.Missed(log, request.RequestId, request.Query.Length);

        var inferred = await classifier.ClassifyAsync(request.Query, request.Slots, findings, cancellationToken);

        return new Specification(request, Requirements(request, Known(request, inferred), inferred: true));
    }

    /// <summary>
    /// The requirements for a slot set, in the order the slots were given.
    /// </summary>
    /// <remarks>
    /// The application's own slot rule decides what a slot is called and how many units it accepts, so
    /// the quantity is clamped against the rule that arrived with the request rather than against a
    /// number remembered here. A slot the application did not send cannot exist, so it is dropped
    /// rather than invented - the table names slots from the closed vocabulary, not from the request.
    /// </remarks>
    private static IReadOnlyList<SlotRequirement> Requirements(
        SuggestionRequest request,
        IReadOnlyList<string> slots,
        bool inferred)
    {
        var rules = request.Slots.ToDictionary(rule => rule.Slot, StringComparer.OrdinalIgnoreCase);

        return
        [
            .. slots
                .Where(rules.ContainsKey)
                .Select(slot => rules[slot])
                .Select(rule => new SlotRequirement(rule.Slot, QuantityRule.For(request.Query, rule), inferred)),
        ];
    }

    /// <summary>What a model answered, reduced to the slots that exist. An unknown slot is dropped.</summary>
    private static IReadOnlyList<string> Known(SuggestionRequest request, IReadOnlyList<string> answered)
    {
        var exists = request.Slots.Select(rule => rule.Slot).ToHashSet(StringComparer.OrdinalIgnoreCase);

        return [.. answered.Where(exists.Contains)];
    }
}
