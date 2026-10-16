using CoreRentalNet.BuildingBlocks.Application.Telemetry;
using CoreRentalNet.Modules.Catalog.Application.Contracts;

namespace CoreRentalNet.Modules.Catalog.Application.Queries.SearchCatalog;

/// <summary>
/// Answers <see cref="ISearchCatalogHandler"/>: the catalogue is narrowed to a scope, and then the name
/// search ranks.
/// </summary>
/// <remarks>
/// <para>
/// <b>The scope narrows and the name search ranks, in that order</b> — the rule the similarity search already
/// follows. What a caller asked for never decides what is eligible, only how it is ordered, so a product the
/// scope excluded cannot be brought back by a good match.
/// </para>
/// <para>
/// <b>Nothing typed is not the same as nothing found.</b> An empty search returns the scope's page in
/// catalogue order, because that is what browsing a category is. Only a real search is handed to the ranker.
/// </para>
/// <para>
/// <b>The store read is asynchronous; the handler stays the operation the UI names.</b> The read is waited
/// on here, exactly as the name search already was, so the pages that call this stay about what they show.
/// </para>
/// </remarks>
public sealed class SearchCatalogHandler(
    IProductCatalogService catalogService,
    IProductNameSearchService catalogNameSearchService) : ISearchCatalogHandler
{
    public IReadOnlyList<ProductDto> Handle(SearchCatalogQuery query)
    {
        ArgumentNullException.ThrowIfNull(query);

        if (query.TypedSearchText is not null && query.ExpandedSearchTerms is not null)
        {
            throw new ArgumentException(
                "A catalogue search narrows by name with what was typed or with the terms a search was given, never both.",
                nameof(query));
        }

        var page = catalogService
            .SearchPageAsync(query.Scope, query.MaximumMonthlyAmount, 0, query.Take, CancellationToken.None)
            .GetAwaiter()
            .GetResult();

        var rankedProducts = Rank(page.Items, query);

        // Recorded at the boundary a customer crosses, not at the HTTP edge: an empty answer is a lost sale, and
        // this is the only place that can tell a real search from the catalogue simply being browsed.
        BusinessTelemetry.CatalogSearches.Add(1);
        BusinessTelemetry.CatalogSearchResults.Record(rankedProducts.Count);

        return rankedProducts;
    }

    /// <summary>Which of the two name searches applies, decided from which one the caller filled in.</summary>
    private IReadOnlyList<ProductDto> Rank(
        IReadOnlyList<ProductDto> productsMatchingTheScope,
        SearchCatalogQuery query)
    {
        if (query.ExpandedSearchTerms is { Count: > 0 } expandedSearchTerms)
        {
            return catalogNameSearchService
                .FindBestMatchesForExpandedSearchTermsAsync(productsMatchingTheScope, expandedSearchTerms)
                .GetAwaiter()
                .GetResult();
        }

        return string.IsNullOrWhiteSpace(query.TypedSearchText)
            ? productsMatchingTheScope
            : catalogNameSearchService
                .FindBestMatchesForTypedSearchWordsAsync(productsMatchingTheScope, query.TypedSearchText)
                .GetAwaiter()
                .GetResult();
    }
}
