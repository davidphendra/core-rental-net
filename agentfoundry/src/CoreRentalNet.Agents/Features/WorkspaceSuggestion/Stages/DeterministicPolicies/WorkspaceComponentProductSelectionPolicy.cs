using CoreRentalNet.Agents.Features.WorkspaceSuggestion.Domain;

namespace CoreRentalNet.Agents.Features.WorkspaceSuggestion.Stages.DeterministicPolicies;

/// <summary>Turns the reranker's judgements into the products the composer may use.</summary>
/// <remarks>
/// <para>
/// <b>It can reorder and select; it cannot introduce or relabel.</b> A SKU it was not given is dropped, and the
/// slot of everything it kept is the pool's — so the product a composed line names is the product a search
/// returned, whatever the model wrote. This is the guard the composer already works under, applied one stage
/// earlier, and it is what lets the rest of the pipeline treat the reranker's answer as a preference.
/// </para>
/// <para>
/// <b>An unmentioned product is a rejected product.</b> The reranker's whole job is to say which few of the pool
/// answer the need; appending the rest would make the ranking advisory and put the pool's width back in front of
/// the composer.
/// </para>
/// </remarks>
public sealed class WorkspaceComponentProductSelectionPolicy(int maximumSelectedProductsPerComponent = 3)
{
    private readonly int _maximumSelectedProductsPerComponent = maximumSelectedProductsPerComponent > 0
        ? maximumSelectedProductsPerComponent
        : throw new ArgumentOutOfRangeException(
            nameof(maximumSelectedProductsPerComponent),
            maximumSelectedProductsPerComponent,
            "The bound must be greater than zero.");

    /// <summary>The products to compose from, ordered as the reranker ordered them.</summary>
    public IReadOnlyList<SelectedWorkspaceComponentProduct> SelectFromPool(
        IReadOnlyList<RetrievedWorkspaceComponentProduct> retrievedProductPool,
        WorkspaceComponentProductRankingResult rankingResult)
    {
        ArgumentNullException.ThrowIfNull(retrievedProductPool);
        ArgumentNullException.ThrowIfNull(rankingResult);

        var selectedProducts = new List<SelectedWorkspaceComponentProduct>();

        foreach (var (componentCategory, assessments) in rankingResult.Assessments.EveryComponent())
        {
            var slot = WorkspaceComponentVocabularyMapping.CompositionSlotFor(componentCategory);
            var productsRetrievedFor = ProductsRetrievedFor(retrievedProductPool, slot);

            selectedProducts.AddRange(assessments
                .Select(assessment => ResolveToPooledProduct(assessment, productsRetrievedFor))
                .OfType<SelectedWorkspaceComponentProduct>()
                .DistinctBy(selection => selection.RetrievedProduct.Sku, StringComparer.OrdinalIgnoreCase)
                .Take(_maximumSelectedProductsPerComponent));
        }

        return selectedProducts;
    }

    /// <summary>The pool's own row for one assessment, or nothing when the product was not in the pool.</summary>
    private static SelectedWorkspaceComponentProduct? ResolveToPooledProduct(
        WorkspaceComponentProductAssessment assessment,
        IReadOnlyList<RetrievedWorkspaceComponentProduct> productsRetrievedFor)
        => productsRetrievedFor
            .FirstOrDefault(product => string.Equals(product.Sku, assessment.Sku, StringComparison.OrdinalIgnoreCase))
            is { } pooledProduct
                ? new SelectedWorkspaceComponentProduct(pooledProduct, assessment.Relevance, assessment.Reason)
                : null;

    /// <summary>The pool's products for one component.</summary>
    private static IReadOnlyList<RetrievedWorkspaceComponentProduct> ProductsRetrievedFor(
        IReadOnlyList<RetrievedWorkspaceComponentProduct> retrievedProductPool,
        Domain.Wire.WorkspaceSlot slot)
        => [.. retrievedProductPool.Where(product => product.Slot == slot)];
}
