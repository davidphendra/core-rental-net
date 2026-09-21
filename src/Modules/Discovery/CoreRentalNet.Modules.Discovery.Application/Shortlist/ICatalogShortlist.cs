namespace CoreRentalNet.Modules.Discovery.Application.Shortlist;

/// <summary>
/// Turns a sentence into the small set of products a run is allowed to choose from.
/// </summary>
/// <remarks>
/// <para>
/// <b>One operation, asynchronous, because embedding the query is a network call.</b> The rest of it is local:
/// the catalogue is already in memory and the vectors are a bounded table, so there is nothing else here that
/// needs to wait.
/// </para>
/// <para>
/// <b>A failure throws.</b> What that means is the caller's decision — a suggestion run reports itself
/// unavailable and the customer retries, which is the same treatment a failed embedding call gets — and
/// folding that meaning into this contract would force it on every other caller.
/// </para>
/// <para>
/// <b>The promise is coverage, not a count.</b> Two products from every bucket the catalogue can fill, which
/// is fourteen for this catalogue and fewer when a bucket is empty. A caller must not read the length as a
/// constant: a slot the catalogue cannot fill is a slot the composition will leave out, and padding the
/// shortlist to reach a number would offer the model products that do not exist.
/// </para>
/// </remarks>
public interface ICatalogShortlist
{
    /// <summary>The products nearest the request, two from each bucket, nearest first within each.</summary>
    Task<IReadOnlyList<ShortlistItem>> ForAsync(string query, CancellationToken cancellationToken);
}
