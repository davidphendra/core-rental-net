using CoreRentalNet.Host.AiBuilder;
using CoreRentalNet.Host.Presentation;
using CoreRentalNet.Modules.Catalog.Application.Contracts;
using CoreRentalNet.Modules.Workspace.Application.Rules;
using CoreRentalNet.Modules.Workspace.Domain;

namespace CoreRentalNet.Host.Agents;

/// <summary>Builds the one payload a run sends: the customer's sentence, the slot rules, and the catalogue.</summary>
/// <remarks>
/// <para>
/// The catalogue is this application's own and the projection is the one it already publishes, so what the
/// agent is shown is what a caller of the catalogue API would read — built in process, never fetched back
/// over the network, never a second description of the same products.
/// </para>
/// <para>
/// <b>Only the query and this projection cross to Foundry.</b> No identity, no address, no draft, no session.
/// The slot rules are sent because the model must not propose a composition the application would refuse;
/// the capacities are configuration, read once at start-up.
/// </para>
/// </remarks>
internal sealed class SuggestionRequestBuilder(IProductCatalog catalogue, WorkspaceSlotSettings slots)
{
    /// <summary>The payload for one run.</summary>
    public SuggestionRequest Build(RunRequest ask)
    {
        ArgumentNullException.ThrowIfNull(ask);

        var query = ask.Query?.Trim() ?? string.Empty;

        return new SuggestionRequest(
            RunId: Guid.NewGuid().ToString("n"),
            Query: query,
            Currency: CompactCatalogProjection.CurrencyOf(catalogue.All),
            CeilingMonthly: ask.CeilingMonthly,
            Slots: EverySlot(),
            Catalogue: [.. catalogue.All.Select(CompactCatalogProjection.Item)]);
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
