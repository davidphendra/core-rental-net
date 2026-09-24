using CoreRentalNet.Host.AiBuilder;
using CoreRentalNet.Host.Presentation;
using CoreRentalNet.Modules.Catalog.Application.Contracts;
using CoreRentalNet.Modules.Workspace.Application.Rules;
using CoreRentalNet.Modules.Workspace.Domain;

namespace CoreRentalNet.Host.Agents;

/// <summary>Builds the one payload a run sends: the customer's sentence and the slot rules.</summary>
/// <remarks>
/// <para>
/// <b>The catalogue is not pushed.</b> A run used to carry either the whole compact projection (~336 KB ≈ 86k
/// tokens, twice, against a deployment that allows 100,000 tokens a minute) or a shortlist retrieved by this
/// application. Both are gone: the agent now reaches the catalogue through the MCP tools the Host publishes,
/// so this builder sends the sentence and the rules and nothing else. The catalogue is read here for exactly
/// one value — the currency the request is stated in.
/// </para>
/// <para>
/// <b>Only the query, the rules and the currency cross to Foundry.</b> No identity, no address, no draft, no
/// session, no SKU. The slot rules are sent because the model must not propose a composition the application
/// would refuse; the capacities are configuration, read once at start-up.
/// </para>
/// </remarks>
internal sealed class SuggestionRequestBuilder(IProductCatalogService catalogue, WorkspaceSlotSettings slots)
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
            Slots: EverySlot());
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
