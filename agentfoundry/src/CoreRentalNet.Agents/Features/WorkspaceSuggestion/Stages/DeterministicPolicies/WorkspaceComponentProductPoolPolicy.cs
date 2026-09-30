using CoreRentalNet.Agents.Features.WorkspaceSuggestion.Domain;

namespace CoreRentalNet.Agents.Features.WorkspaceSuggestion.Stages.DeterministicPolicies;

/// <summary>Bounds how many products each component may offer the reranker.</summary>
/// <remarks>
/// <para>
/// <b>Fifteen, and measured rather than chosen.</b> Run over the real catalogue with the terms a requirement
/// expansion produces, the product a need describes lands within the top fifteen for every need the search can
/// find at all — #1, #1, #1, #2, #4, #5, #11. A single broad category term needs thirty, because its order is not
/// a relevance order; the terms are what make fifteen enough, and <c>NameSearchPoolCharacterisationTests</c> is
/// where those numbers live.
/// </para>
/// <para>
/// <b>Per component rather than per run.</b> A single total would be spent on whichever component's terms matched
/// most, and the reranker would be handed a pool with no seating in it — so every component is represented, or has
/// nothing to represent.
/// </para>
/// </remarks>
public sealed class WorkspaceComponentProductPoolPolicy(int maximumRetrievedProductsPerComponent = 15)
{
    private readonly int _maximumRetrievedProductsPerComponent = maximumRetrievedProductsPerComponent > 0
        ? maximumRetrievedProductsPerComponent
        : throw new ArgumentOutOfRangeException(
            nameof(maximumRetrievedProductsPerComponent),
            maximumRetrievedProductsPerComponent,
            "The bound must be greater than zero.");

    /// <summary>One product per SKU, at most the bound for each component, in search order.</summary>
    public IReadOnlyList<RetrievedWorkspaceComponentProduct> ApplyPoolBounds(
        IReadOnlyList<RetrievedWorkspaceComponentProduct> retrievedProducts)
    {
        ArgumentNullException.ThrowIfNull(retrievedProducts);

        var seenStockKeepingUnits = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var boundedPool = new List<RetrievedWorkspaceComponentProduct>();

        foreach (var componentGroup in retrievedProducts.GroupBy(product => product.Slot))
        {
            boundedPool.AddRange(componentGroup
                .Where(product => seenStockKeepingUnits.Add(product.Sku))
                .Take(_maximumRetrievedProductsPerComponent));
        }

        return boundedPool;
    }
}
