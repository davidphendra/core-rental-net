namespace CoreRentalNet.Modules.Discovery.Application.Shortlist;

/// <summary>
/// Chooses the shortlist: the nearest few products from every bucket the catalogue can fill.
/// </summary>
/// <remarks>
/// <para>
/// <b>Per bucket and not simply nearest-overall, and that is the whole point.</b> This catalogue is 205
/// products of which 150 are accessories, so a global "nearest fourteen" can legitimately be fourteen lamps
/// and no desk at all — a shortlist that covers nothing the customer asked for while looking perfectly
/// healthy. Taking a fixed number from each bucket makes coverage a property of the shape rather than a hope
/// about the data.
/// </para>
/// <para>
/// <b>Pure, and given its candidates rather than fetching them.</b> Every rule above is decided here, and a
/// rule that can only be exercised through SQLite is a rule that gets tested once, coarsely, in the tier that
/// has a file.
/// </para>
/// </remarks>
public static class CatalogRanking
{
    /// <summary>The nearest <paramref name="perBucket"/> products from each bucket, nearest first within each.</summary>
    /// <remarks>
    /// Buckets come back in the catalogue's own order — category, then subcategory — so the same inputs always
    /// produce the same sequence. Inside a bucket, equal scores are broken by SKU for the same reason: a
    /// shortlist whose order depends on dictionary iteration is one nobody can reproduce when a run is
    /// questioned.
    /// </remarks>
    public static IReadOnlyList<ShortlistItem> TopPerBucket(
        float[] query,
        IReadOnlyList<CatalogCandidate> candidates,
        int perBucket,
        SelectionBoost? boost = null)
    {
        ArgumentNullException.ThrowIfNull(query);
        ArgumentNullException.ThrowIfNull(candidates);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(perBucket);

        return
        [
            .. candidates
                .GroupBy(candidate => candidate.Bucket)
                .OrderBy(bucket => bucket.Key.Category)
                .ThenBy(bucket => bucket.Key.SubCategory)
                .SelectMany(bucket => Rank(query, bucket, perBucket, boost)),
        ];
    }

    /// <summary>
    /// The products a bucket contributes: chosen by similarity, then ordered by what customers have taken.
    /// </summary>
    /// <remarks>
    /// <b>Two steps, in that order, and the order is the safety rule.</b> Selecting by similarity first means the
    /// boost can only rearrange products that were already going to travel — it cannot move one out, so it cannot
    /// starve a product that has never been chosen. Doing it the other way round would close the loop: not shown,
    /// therefore never chosen, therefore a worse ratio, therefore not shown again.
    /// </remarks>
    private static IEnumerable<ShortlistItem> Rank(
        float[] query,
        IGrouping<CatalogBucket, CatalogCandidate> bucket,
        int perBucket,
        SelectionBoost? boost)
        => bucket
            .Select(candidate => new ShortlistItem(candidate.Sku, candidate.Bucket, Cosine(query, candidate.Embedding)))
            .OrderByDescending(item => item.Score)
            .ThenBy(item => item.Sku, StringComparer.Ordinal)
            .Take(perBucket)
            // The reported score stays the similarity: it is what the contract says it is, and what a caller may
            // order by. The ORDER is what carries the signal.
            .OrderByDescending(item => boost?.Score(item.Score, item.Sku) ?? item.Score)
            .ThenBy(item => item.Sku, StringComparer.Ordinal);

    /// <summary>How near two vectors are, as a cosine similarity between -1 and 1.</summary>
    /// <remarks>
    /// A vector of zeros has no direction and no angle to anything, so it scores zero instead of dividing by
    /// zero. That is a real case rather than a defensive one: an embedding deployment that answered with zeros
    /// would otherwise turn the whole ranking into <c>NaN</c>, and <c>NaN</c> compares false against everything,
    /// so the order would become whatever the sort happened to do.
    /// </remarks>
    private static float Cosine(float[] query, float[] vector)
    {
        if (query.Length != vector.Length)
        {
            throw new InvalidOperationException(
                $"A candidate vector is {vector.Length} wide and the query is {query.Length}. "
                + "Vectors of two widths cannot be compared, and an index of the wrong width is what the freshness check refuses.");
        }

        double dot = 0;
        double queryNorm = 0;
        double vectorNorm = 0;

        for (var position = 0; position < query.Length; position++)
        {
            dot += query[position] * vector[position];
            queryNorm += query[position] * query[position];
            vectorNorm += vector[position] * vector[position];
        }

        var scale = Math.Sqrt(queryNorm) * Math.Sqrt(vectorNorm);

        return scale == 0 ? 0f : (float)(dot / scale);
    }
}
