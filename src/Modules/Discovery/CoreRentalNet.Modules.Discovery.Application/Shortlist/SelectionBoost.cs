namespace CoreRentalNet.Modules.Discovery.Application.Shortlist;

/// <summary>
/// What customers' actual choices add to a product's similarity.
/// </summary>
/// <remarks>
/// <para>
/// <b>Additive and bounded, never subtractive.</b> The score is the similarity plus a term between zero and
/// <see cref="Weight"/>, because the ratio it is built on is between zero and one. A product nobody has chosen
/// yet is not punished for it — it simply gets nothing added — which is the difference between a ranking that
/// learns and one that decides a product is bad for being new.
/// </para>
/// <para>
/// <b>It ORDERS the shortlist and never selects it.</b> The candidates are chosen by similarity first and only
/// then reordered, and that ordering is the whole reason the feedback loop cannot close: a boost that could move
/// a product out of the shortlist would mean not shown, therefore never chosen, therefore offered without a
/// choice, therefore a lower score, therefore not shown again. Reordering is what the signal is allowed to do.
/// </para>
/// <para>
/// A product with no row scores zero rather than dividing by nothing: it has never been offered, so there is no
/// ratio to express.
/// </para>
/// </remarks>
public sealed record SelectionBoost(double Weight, IReadOnlyDictionary<string, double> Ratios)
{
    /// <summary>How often a product was chosen of the times it was offered, or zero when it never was.</summary>
    public double RatioOf(string sku)
        => Ratios.TryGetValue(sku, out var ratio) ? ratio : 0d;

    /// <summary>The number a product is ordered by: its similarity, plus what customers have done with it.</summary>
    public double Score(double similarity, string sku)
        => similarity + (Weight * RatioOf(sku));
}
