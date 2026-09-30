using CoreRentalNet.BuildingBlocks.Application.Embeddings;
using CoreRentalNet.Modules.Catalog.Application.Contracts;

namespace CoreRentalNet.Modules.Catalog.Application.Queries.SearchSimilarityCatalog;

/// <summary>
/// Answers a similarity search: the catalogue narrowed by its filters, then ordered by how near each product
/// is to the sentence.
/// </summary>
/// <remarks>
/// <para>
/// <b>The filters narrow before the search orders.</b> A request for desks asks which desks are nearest,
/// not which products are nearest and then happen to be desks; doing it the other way round would let a desk
/// fall out of a top-N that had already been chosen from every category.
/// </para>
/// <para>
/// <b>The sentence is embedded here, and the vector is what is searched with.</b> The port takes a vector
/// rather than a sentence, so this is the one place the request becomes a number, and the one place an
/// embedding outage is turned into <see cref="ProductSimilarityUnavailableException"/>.
/// </para>
/// <para>
/// <b>A product with no stored vector cannot be ranked, so it is left out.</b> The freshness check is what
/// makes that unreachable in a deployment - an index missing products is not the index this build would make -
/// but the rule is stated here rather than assumed behind it.
/// </para>
/// <para>
/// <b>Distance first and SKU second, so equal distances always produce the same order</b> - a cosine distance
/// has no ties to speak of, but an order that depended on the file's iteration order is one nobody can
/// reproduce when an answer is questioned.
/// </para>
/// </remarks>
public sealed class SearchSimilarityCatalogHandler(
    IProductCatalogService catalogueService,
    IEmbeddingService embeddingService,
    IProductSimilarityService productSimilarityService) : ISearchSimilarityCatalogHandler
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<ProductView>> HandleAsync(
        SearchSimilarityCatalogQuery query,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        ArgumentException.ThrowIfNullOrWhiteSpace(query.Query);

        var eligible = catalogueService.Search(query.Category, query.SubCategory, query.MaximumMonthlyAmount);
        var queryVectors = await embeddingService.EmbedAsync([query.Query], cancellationToken);
        var nearestCatalogsVector = await productSimilarityService.NearestAsync(queryVectors[0], cancellationToken);

        var bySku = nearestCatalogsVector.ToDictionary(
            product => product.Sku,
            product => product.Distance,
            StringComparer.OrdinalIgnoreCase);

        var nearesCatalogs = eligible
            .Where(product => bySku.ContainsKey(product.Sku))
            .OrderBy(product => bySku[product.Sku])
            .ThenBy(product => product.Sku, StringComparer.Ordinal);

        return [..nearesCatalogs];
    }
}
