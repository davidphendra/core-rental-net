namespace CoreRentalNet.Modules.Discovery.Application.Selection;

/// <summary>Records that a set of products was shown to the model, so the score has a denominator.</summary>
/// <remarks>
/// <para>
/// <b>An offer means the request reached the model, and nothing weaker.</b> A shortlist that was built and never
/// sent — because the embedding call failed, or the agent was unreachable, or the run stopped before the agent
/// began — was offered to nobody, and counting it would put products into the denominator that no one ever
/// considered. That inflates the denominator and pushes every score down, which reads as "customers do not like
/// anything" rather than as the bug it is.
/// </para>
/// <para>
/// The denominator is also what makes the score comparable as the catalogue changes: a product offered in every
/// run and chosen in half of them scores a half, however many runs there were.
/// </para>
/// </remarks>
public interface IOfferSelection
{
    /// <summary>Records each distinct SKU as offered once, and returns how many were recorded.</summary>
    Task<int> OfferAsync(IReadOnlyList<string> skus, CancellationToken cancellationToken);
}
