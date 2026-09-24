using CoreRentalNet.Modules.Catalog.Application.Contracts;

namespace CoreRentalNet.Modules.Catalog.Application.Queries.SearchCatalog;

/// <summary>
/// Answers <see cref="ISearchCatalogHandler"/>: the catalogue is narrowed, and then the name search ranks.
/// </summary>
/// <remarks>
/// <para>
/// <b>The filters narrow and the name search ranks, in that order</b> — the rule the similarity search already
/// follows. What was typed never decides what is eligible, only how it is ordered, so a product the filters
/// excluded cannot be brought back by a good match.
/// </para>
/// <para>
/// <b>Nothing typed is not the same as nothing found.</b> An empty search returns everything the filters
/// allow, in catalogue order, because that is what browsing a category is — the store, the picker and the
/// panel all open that way. Only a real search is handed to the ranker.
/// </para>
/// </remarks>
public sealed class SearchCatalogHandler(
    IProductCatalogService catalogService,
    IProductNameSearchService catalogNameSearchService) : ISearchCatalogHandler
{
    public IReadOnlyList<ProductView> Handle(SearchCatalogQuery query)
    {
        ArgumentNullException.ThrowIfNull(query);

        var productsMatchingTheFilters = catalogService.Search(query.Category, query.SubCategory);

        return string.IsNullOrWhiteSpace(query.Search)
            ? productsMatchingTheFilters
            : catalogNameSearchService.FindBestMatchesAsync(productsMatchingTheFilters, query.Search)
                .GetAwaiter()
                .GetResult();
    }
}
