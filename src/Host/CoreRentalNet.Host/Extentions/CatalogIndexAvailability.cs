namespace CoreRentalNet.Host.Extentions;

/// <summary>
/// Whether the stored catalogue vectors may be searched at all, and why not when they may not.
/// </summary>
/// <remarks>
/// <para>
/// <b>A singleton with one mutable fact, and it is mutable because the answer arrives after the container is
/// built.</b> The freshness check reads the ingestion tool's file at start-up, long after registration, so the
/// read records its answer here instead of rebuilding the container.
/// </para>
/// <para>
/// <b>It starts usable and can only be turned off</b>: a deployment whose index happens to be unreadable
/// refuses a similarity search rather than ranking on vectors it cannot explain, and nothing turns a refused
/// index back on at run time.
/// </remarks>
internal sealed class CatalogIndexAvailability
{
    /// <summary>True until the freshness check says otherwise.</summary>
    public bool IsUsable { get; private set; } = true;

    /// <summary>The reason the index cannot be searched. Empty while it can.</summary>
    public string Reason { get; private set; } = string.Empty;

    /// <summary>Refuses the index, recording why. Idempotent, and it never re-opens.</summary>
    public void Refuse(string reason)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);

        IsUsable = false;
        Reason = reason;
    }
}
