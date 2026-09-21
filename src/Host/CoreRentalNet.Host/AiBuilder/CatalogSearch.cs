using CoreRentalNet.Modules.Discovery.Application.Shortlist;

namespace CoreRentalNet.Host.AiBuilder;

/// <summary>
/// The catalogue search, and what the run says when it cannot be made.
/// </summary>
/// <remarks>
/// <para>
/// It owns one phase of a run: the hop the whole e06 epic added, and the two ways it can end without a result —
/// the deployment refusing, and the run's own deadline expiring. Both of them end the run rather than falling
/// back, which is why they are decided together here rather than at each call site.
/// </para>
/// <para>
/// <b>Nothing here falls back to the whole catalogue.</b> That is the behaviour this epic exists to remove: it
/// re-breaches the measured 100,000-tokens-a-minute ceiling. Nor to a name search, which would turn "we could not
/// retrieve" into "here is a bad shortlist" — the failure nobody reports.
/// </para>
/// </remarks>
internal sealed class CatalogSearch(ICatalogShortlist shortlist, SuggestionEventStream stream)
{
    /// <summary>The products retrieval chose, or null when it could not choose any and the run is over.</summary>
    public async Task<IReadOnlyList<ShortlistItem>?> ProductsAsync(string query, RunBudget budget)
    {
        try
        {
            return await shortlist.ForAsync(query, budget.Run);
        }
        catch (ShortlistUnavailableException)
        {
            await stream.FailedAsync(SuggestionEventStream.Unavailable, budget.Customer);
            return null;
        }
    }

    /// <summary>Tells the customer the run ran out of time, on a token that is not the spent one.</summary>
    /// <remarks>
    /// The run's token is cancelled by definition here, so writing with it would fail before a byte was sent. The
    /// customer's token is still live — they are waiting — and is what the response was opened with. This is
    /// best effort: the run is already over and its record already says what happened, so a failure to tell the
    /// page must not replace the reason the run ended.
    /// </remarks>
    public async Task TimedOutAsync(RunBudget budget)
    {
        try
        {
            await stream.FailedAsync(SuggestionEventStream.Unavailable, budget.Customer);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
        }
    }
}
