using CoreRentalNet.Modules.Catalog.Application.Contracts;

namespace CoreRentalNet.Modules.Catalog.Application.Queries.SearchCatalog;

/// <summary>
/// Answers <see cref="ISearchCatalogHandler"/>: the catalogue is narrowed, and then the name search ranks.
/// </summary>
/// <remarks>
/// <para>
/// <b>The filters narrow and the name search ranks, in that order</b> — the rule the similarity search already
/// follows. What a caller asked for never decides what is eligible, only how it is ordered, so a product the
/// filters excluded cannot be brought back by a good match.
/// </para>
/// <para>
/// <b>Nothing typed is not the same as nothing found.</b> An empty search returns everything the filters
/// allow, in catalogue order, because that is what browsing a category is — the store, the picker and the
/// panel all open that way. Only a real search is handed to the ranker.
/// </para>
/// <para>
/// <b>Which of the two name searches applies is decided here, from which one the caller filled in, and a query
/// that filled in both is refused.</b> The rules differ — a typed search requires every word, an expanded
/// search requires any term — so a precedence here would be this class guessing at an intent the caller
/// expressed twice.
/// </para>
/// </remarks>
public sealed class SearchCatalogHandler(
    IProductCatalogService catalogService,
    IProductNameSearchService catalogNameSearchService) : ISearchCatalogHandler
{
    public IReadOnlyList<ProductView> Handle(SearchCatalogQuery query)
    {
        ArgumentNullException.ThrowIfNull(query);

        if (query.TypedSearchText is not null && query.ExpandedSearchTerms is not null)
        {
            throw new ArgumentException(
                "A catalogue search narrows by name with what was typed or with the terms a search was given, never both.",
                nameof(query));
        }

        var productsMatchingTheFilters = catalogService.Search(
            query.Category,
            query.SubCategory,
            query.MaximumMonthlyAmount);

        if (query.ExpandedSearchTerms is { Count: > 0 } expandedSearchTerms)
        {
            return catalogNameSearchService
                .FindBestMatchesForExpandedSearchTermsAsync(productsMatchingTheFilters, expandedSearchTerms)
                .GetAwaiter()
                .GetResult();
        }

        return string.IsNullOrWhiteSpace(query.TypedSearchText)
            ? productsMatchingTheFilters
            : catalogNameSearchService
                .FindBestMatchesForTypedSearchWordsAsync(productsMatchingTheFilters, query.TypedSearchText)
                .GetAwaiter()
                .GetResult();
    }
}
