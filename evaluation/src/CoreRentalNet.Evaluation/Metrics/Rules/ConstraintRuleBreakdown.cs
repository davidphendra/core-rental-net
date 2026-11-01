using CoreRentalNet.Evaluation.Runs.Record;
using CoreRentalNet.Evaluation.Runs.Storage;
using CoreRentalNet.Evaluation.Runs.Result;
using CoreRentalNet.Evaluation.Runs.Assembly;

using CoreRentalNet.Evaluation.Metrics.Outcome;
using CoreRentalNet.Evaluation.Metrics.Retrieval;
using CoreRentalNet.Evaluation.Metrics.Statistics;
using CoreRentalNet.Evaluation.Metrics.Thresholds;
namespace CoreRentalNet.Evaluation.Metrics.Rules;

/// <summary>Compliance per rule, so a report can name which limit a recommendation broke.</summary>
/// <remarks>
/// The quantities come from the recommendation itself, never from the model's prose. This runs without a judge
/// and without a network call, which is what makes it usable as a gate.
/// </remarks>
public static class ConstraintRuleBreakdown
{
    private static readonly (string Rule, int Min, int Max)[] Rules =
    [
        ("desk", 1, 1),
        ("chair", 1, 1),
        ("monitor", 0, 3),
        ("lamp", 0, 1),
        ("plant", 0, 1),
        ("bean bag", 0, 1),
        ("coffee machine", 0, 1),
    ];

    public static ConstraintRuleReport Evaluate(IReadOnlyList<RagSuggestedLine> lines)
    {
        ArgumentNullException.ThrowIfNull(lines);

        var quantities = Quantities(lines);
        var results = Rules
            .Select(rule => Result(rule.Rule, rule.Min, rule.Max, quantities.GetValueOrDefault(rule.Rule)))
            .ToArray();

        return new ConstraintRuleReport(results.All(result => result.Compliant), results);
    }

    /// <summary>The quantity per rule, exposed so a composition can be compared with an expectation.</summary>
    public static IReadOnlyDictionary<string, int> Totals(IReadOnlyList<RagSuggestedLine> lines)
    {
        ArgumentNullException.ThrowIfNull(lines);
        return Quantities(lines);
    }

    private static Dictionary<string, int> Quantities(IReadOnlyList<RagSuggestedLine> lines)
    {
        var quantities = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        foreach (var line in lines.Where(line => line.Quantity > 0))
        {
            var rule = RuleName(line.Slot);
            quantities[rule] = quantities.GetValueOrDefault(rule) + line.Quantity;
        }

        return quantities;
    }

    private static string RuleName(string slot) => slot.Trim() switch
    {
        "Desk" => "desk",
        "Chair" => "chair",
        "Monitor" or "Monitors" => "monitor",
        "Lamp" => "lamp",
        "Plant" => "plant",
        "RelaxZone" or "Bean Bag" => "bean bag",
        "CoffeeStation" or "Coffee Machine" => "coffee machine",
        var unknown => unknown.ToLowerInvariant(),
    };

    private static ConstraintRuleResult Result(string rule, int minimum, int maximum, int actual)
        => new(rule, minimum, maximum, actual, actual >= minimum && actual <= maximum);
}

/// <summary>One rule's verdict for one recommendation.</summary>
public sealed record ConstraintRuleResult(string Rule, int Minimum, int Maximum, int Actual, bool Compliant);

/// <summary>The rule table applied to one recommendation.</summary>
public sealed record ConstraintRuleReport(bool Compliant, IReadOnlyList<ConstraintRuleResult> Rules)
{
    /// <summary>The rules the recommendation broke, by name.</summary>
    public IReadOnlyList<string> Violations { get; } =
        [.. Rules.Where(rule => !rule.Compliant).Select(rule => rule.Rule)];
}
