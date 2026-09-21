namespace CoreRentalNet.Modules.Discovery.Infrastructure;

/// <summary>
/// What the index was built from. One row per named index, and today exactly one row.
/// </summary>
/// <remarks>
/// <para>
/// These four values are recorded rather than assumed because each of them silently invalidates every vector
/// in the table when it changes. An index built by one model cannot be searched with a query embedded by
/// another; an index built at one width cannot be compared with a query at another; an index built from a
/// different composition of a product's text is not comparable with one built from this composition; and an
/// index built from an older <c>products.json</c> describes products that may no longer exist or no longer
/// cost what they cost.
/// </para>
/// <para>
/// Storing them turns each of those from a wrong answer into a detectable fact, which is the difference
/// between a stale index and a silent one.
/// </para>
/// </remarks>
public sealed class CatalogIndex
{
    /// <summary>Which index this row describes. One today, and named rather than numbered so a reader of
    /// the table can see what it is.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>The embedding deployment the vectors came from, e.g. <c>text-embedding-3-large</c>.</summary>
    public string ModelId { get; set; } = string.Empty;

    /// <summary>How many floats each vector holds. Recorded rather than read from configuration at query
    /// time, because a changed setting would otherwise compare a 512-wide row with a differently-wide query.</summary>
    public int Width { get; set; }

    /// <summary>The composition of each product's embedded text — which fields, in which order — as a stable
    /// value. It is compared, never parsed.</summary>
    public string Composition { get; set; } = string.Empty;

    /// <summary>The hash of <c>products.json</c> as it was read. A change to the file is a change to what the
    /// index describes.</summary>
    public string CatalogueHash { get; set; } = string.Empty;
}
