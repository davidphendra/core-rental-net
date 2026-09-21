namespace CoreRentalNet.Modules.Discovery.Infrastructure.Selection;

/// <summary>
/// One product's selection record: how often it was offered, how often it was chosen, and when it last moved.
/// </summary>
/// <remarks>
/// <para>
/// <b>Two decaying counters rather than a list of events.</b> The score that will rank future shortlists is a
/// ratio, and a ratio of two accumulators is bounded and needs no history: a table of every offer ever made
/// would grow without limit to answer a question that one row per product answers exactly.
/// </para>
/// <para>
/// <b>No customer and no query is stored.</b> A count of how often a product was offered and chosen cannot be
/// tied to a person or to what they typed, which is what keeps this out of the run log's retention question
/// rather than inside it.
/// </para>
/// <para>
/// <c>LastUpdatedUtc</c> is what makes decay lazy: a count is decayed when it is next written, so nothing has to
/// run on a schedule, and a row nobody touches simply keeps the ratio it had.
/// </para>
/// </remarks>
public sealed class CatalogSelection
{
    /// <summary>The product this row is about. The catalogue's own key, as elsewhere.</summary>
    public string Sku { get; set; } = string.Empty;

    /// <summary>How many times it was offered, decayed. Written by the retrieval path.</summary>
    public double DecayedOffered { get; set; }

    /// <summary>How many times it was chosen, decayed. Written by the apply path.</summary>
    public double DecayedChosen { get; set; }

    /// <summary>When either counter was last moved, so the next write knows how much to decay by.</summary>
    public DateTimeOffset LastUpdatedUtc { get; set; }
}
