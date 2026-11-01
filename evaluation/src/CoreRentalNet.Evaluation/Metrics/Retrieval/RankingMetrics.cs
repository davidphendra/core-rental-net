using CoreRentalNet.Evaluation.Metrics.Outcome;
using CoreRentalNet.Evaluation.Metrics.Statistics;
using CoreRentalNet.Evaluation.Metrics.Rules;
using CoreRentalNet.Evaluation.Metrics.Thresholds;

namespace CoreRentalNet.Evaluation.Metrics.Retrieval;

/// <summary>Rank-sensitive retrieval metrics over the ranked candidates and a labelled relevant set.</summary>
/// <remarks>
/// Every method returns null when the case carries no relevance labels, because a rate over an empty denominator
/// is not zero — it is unknown. A null here is what makes a report say "not evaluable" instead of printing 0%.
/// </remarks>
public static class RankingMetrics
{
    /// <summary>Relevant items found in the top K over all relevant items.</summary>
    public static double? RecallAtK(
        IReadOnlyCollection<string> relevant,
        IReadOnlyList<string> ranked,
        int k)
    {
        ArgumentNullException.ThrowIfNull(relevant);
        ArgumentNullException.ThrowIfNull(ranked);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(k);

        if (relevant.Count == 0)
        {
            return null;
        }

        var hits = Top(ranked, k).Count(relevant.Contains);
        return hits / (double)relevant.Count;
    }

    /// <summary>Relevant items found in the top K over K, the conventional fixed-denominator form.</summary>
    public static double? PrecisionAtK(
        IReadOnlyCollection<string> relevant,
        IReadOnlyList<string> ranked,
        int k)
    {
        ArgumentNullException.ThrowIfNull(relevant);
        ArgumentNullException.ThrowIfNull(ranked);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(k);

        if (relevant.Count == 0)
        {
            return null;
        }

        return Top(ranked, k).Count(relevant.Contains) / (double)k;
    }

    /// <summary>Discounted gain, normalised by the best ordering the labels allow.</summary>
    public static double? NdcgAtK(
        IReadOnlyDictionary<string, int> grades,
        IReadOnlyList<string> ranked,
        int k)
    {
        ArgumentNullException.ThrowIfNull(grades);
        ArgumentNullException.ThrowIfNull(ranked);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(k);

        if (grades.Count == 0)
        {
            return null;
        }

        var dcg = Top(ranked, k).Select((sku, index) => Gain(grades, sku) / Discount(index)).Sum();
        var ideal = grades.Values.OrderByDescending(grade => grade)
            .Take(k)
            .Select((grade, index) => grade / Discount(index))
            .Sum();

        return ideal == 0 ? null : dcg / ideal;
    }

    private static IEnumerable<string> Top(IReadOnlyList<string> ranked, int k)
        => ranked.Take(k);

    private static int Gain(IReadOnlyDictionary<string, int> grades, string sku)
        => grades.GetValueOrDefault(sku);

    /// <summary>The position discount, with ranks counted from one.</summary>
    private static double Discount(int index) => Math.Log2(index + 2);
}
