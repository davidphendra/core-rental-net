using CoreRentalNet.Host.AiBuilder;
using CoreRentalNet.Host.Presentation;
using CoreRentalNet.Modules.Catalog.Application.Contracts;
using CoreRentalNet.Modules.Discovery.Application.Shortlist;
using CoreRentalNet.Modules.Workspace.Application.Rules;
using CoreRentalNet.Modules.Workspace.Domain;

namespace CoreRentalNet.Host.Agents;

/// <summary>Builds the one payload a run sends: the customer's sentence, the slot rules, and the shortlist.</summary>
/// <remarks>
/// <para>
/// The catalogue is this application's own and the projection is the one it already publishes, so what the
/// agent is shown is what a caller of the catalogue API would read — built in process, never fetched back
/// over the network, never a second description of the same products.
/// </para>
/// <para>
/// <b>The catalogue is no longer the whole of it.</b> e06 replaced the pushed projection with the products
/// retrieval chose — two from each bucket, fourteen in all — because a run was carrying ~336 KB twice against a
/// deployment that allows 100,000 tokens a minute. The shortlist arrives as an argument rather than being
/// fetched here: this class is a projection and holds no I/O, which is what lets its boundaries be asserted
/// without a network.
/// </para>
/// <para>
/// <b>Only the query and this projection cross to Foundry.</b> No identity, no address, no draft, no session.
/// The slot rules are sent because the model must not propose a composition the application would refuse;
/// the capacities are configuration, read once at start-up.
/// </para>
/// </remarks>
internal sealed class SuggestionRequestBuilder(IProductCatalog catalogue, WorkspaceSlotSettings slots)
{
    /// <summary>The payload for one run, built from the products retrieval chose.</summary>
    public SuggestionRequest Build(RunRequest ask, IReadOnlyList<ShortlistItem> shortlist)
    {
        ArgumentNullException.ThrowIfNull(ask);
        ArgumentNullException.ThrowIfNull(shortlist);

        var query = ask.Query?.Trim() ?? string.Empty;

        return new SuggestionRequest(
            RunId: Guid.NewGuid().ToString("n"),
            Query: query,
            Currency: CompactCatalogProjection.CurrencyOf(catalogue.All),
            CeilingMonthly: ask.CeilingMonthly,
            Slots: EverySlot(),
            Catalogue: [.. shortlist.Select(item => CompactCatalogProjection.Item(Product(item.Sku)))]);
    }

    /// <summary>
    /// The whole product behind a shortlisted SKU.
    /// </summary>
    /// <remarks>
    /// The shortlist carries identifiers and nothing else, because the catalogue is the one owner of a name and
    /// a price; this is where the two meet. A SKU that cannot be found means the index describes a catalogue
    /// that has changed — retrieval already refuses that case, and this refuses it a second time rather than
    /// building a payload with a product missing from it, because a silently shorter catalogue is a suggestion
    /// drawn from less than the model was told it had.
    /// </remarks>
    private ProductView Product(string sku)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sku);

        return catalogue.Find(sku)
            ?? throw new InvalidOperationException(
                $"The shortlist names '{sku}', which the catalogue does not hold. The index describes a catalogue that has changed; re-run CoreRentalNet.CatalogIngestion.");
    }

    /// <summary>
    /// Every slot the domain has, with the capacity configuration gives it.
    /// </summary>
    /// <remarks>
    /// Every slot rather than the slots a draft is using: the agent composes a whole workspace, and the rule
    /// it is told is the rule the application will check it against. Taken from the enum, so a slot added to
    /// the domain is one the agent is told about rather than one it silently ignores.
    /// </remarks>
    private IReadOnlyList<SuggestionSlotRule> EverySlot()
        => [.. Enum.GetValues<SlotId>().Select(slot => new SuggestionSlotRule(slot, slots.CapacityFor(slot)))];
}
