namespace CoreRentalNet.Modules.Discovery.Application.Indexing;

/// <summary>
/// Whether the index can be searched, and what to say when it cannot.
/// </summary>
/// <remarks>
/// <para>
/// Three outcomes rather than a bool, because two of them look the same to a caller and mean different things
/// to whoever has to fix it: an index that was <b>never built</b> wants the ingestion tool run, and an index
/// that has gone <b>stale</b> wants to know which of the four recorded values moved. Both hide the AI section;
/// the line written at start-up is the difference.
/// </para>
/// <para>
/// <see cref="Reason"/> never reaches a customer. It is written to the log, where an operator reads it, and
/// the tool it names is the one they then run.
/// </para>
/// </remarks>
public sealed record CatalogIndexVerdict
{
    private CatalogIndexVerdict(bool isCurrent, string reason)
    {
        IsCurrent = isCurrent;
        Reason = reason;
    }

    /// <summary>True when the index matches what the deployment is configured to use.</summary>
    public bool IsCurrent { get; }

    /// <summary>Why it cannot be searched. Empty when it can.</summary>
    public string Reason { get; }

    /// <summary>The index is what this deployment would build.</summary>
    public static CatalogIndexVerdict Usable { get; } = new(true, string.Empty);

    /// <summary>No index exists: nothing has ever been ingested into this database.</summary>
    public static CatalogIndexVerdict NotBuilt { get; } = new(
        false,
        "the catalogue index does not exist in this database; run CoreRentalNet.CatalogIngestion to build it");

    /// <summary>The index exists and was built from something else.</summary>
    public static CatalogIndexVerdict Stale(string reason)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);

        return new CatalogIndexVerdict(false, reason);
    }
}
