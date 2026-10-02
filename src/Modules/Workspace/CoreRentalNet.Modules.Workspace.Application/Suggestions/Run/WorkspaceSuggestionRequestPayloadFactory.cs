using CoreRentalNet.Modules.Catalog.Application.Contracts;
using CoreRentalNet.Modules.Workspace.Application.Rules;
using CoreRentalNet.Modules.Workspace.Application.Suggestions.Agent;
using CoreRentalNet.Modules.Workspace.Domain;

namespace CoreRentalNet.Modules.Workspace.Application.Suggestions.Run;

/// <summary>Builds the one payload a run sends: the customer's sentence and the slot rules.</summary>
/// <remarks>
/// <para>
/// <b>The catalogue is not pushed.</b> The agent reaches the catalogue through the MCP tools the Host
/// publishes, so this sends the sentence and the rules and nothing else. The catalogue is read here for
/// exactly one value - the currency the request is stated in.
/// </para>
/// <para>
/// <b>Only the query, the rules and the currency cross to the agent.</b> No identity, no address, no draft, no
/// session, no SKU. The token is the caller's, taken from the sign-in session, and it is left out of the
/// payload hash because the hash identifies what the run was drawn from rather than who asked.
/// </para>
/// </remarks>
public sealed class WorkspaceSuggestionRequestPayloadFactory(
    IProductCatalogService productCatalogService,
    WorkspaceSlotSettings workspaceSlotSettings) : IWorkspaceSuggestionRequestFactory
{
    /// <remarks>
    /// <b>Nothing here is a secret.</b> The caller's token used to be a member of this payload; it now travels on
    /// the invocation, so the run's document is the sentence, the rules and the currency — and there is no rule
    /// left to remember about keeping a credential out of the hash.
    /// </remarks>
    public WorkspaceSuggestionRequestPayload Create(WorkspaceSuggestionQuery suggestionQuery)
        => new(
            RunId: Guid.NewGuid().ToString("n"),
            Query: suggestionQuery.Query.Trim(),
            Currency: CurrencyOf(productCatalogService.All),
            CeilingMonthly: suggestionQuery.CeilingMonthly,
            Slots: EverySlotRule());

    /// <summary>The currency a run thinks in, read from the catalogue's own products.</summary>
    private static string CurrencyOf(IReadOnlyList<ProductView> everyProduct)
        => everyProduct.Count == 0 ? "IDR" : everyProduct[0].MonthlyPrice.Currency;

    /// <summary>Every slot the domain has, with the capacity configuration gives it.</summary>
    private IReadOnlyList<WorkspaceSuggestionRequestSlotRule> EverySlotRule()
        => [.. Enum.GetValues<SlotId>().Select(slot => new WorkspaceSuggestionRequestSlotRule(slot, workspaceSlotSettings.CapacityFor(slot)))];
}
