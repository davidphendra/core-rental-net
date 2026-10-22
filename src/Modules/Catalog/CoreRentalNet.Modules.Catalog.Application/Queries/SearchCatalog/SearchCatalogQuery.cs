using CoreRentalNet.Modules.Catalog.Application.Contracts;

namespace CoreRentalNet.Modules.Catalog.Application.Queries.SearchCatalog;

/// <summary>
/// Lists products in one scope, optionally narrowed by a monthly ceiling and ranked by name. This is
/// the query behind the Builder's category tabs, its slot picker and the Store page. Results preserve
/// catalog order unless a name search ranks them.
/// </summary>
/// <remarks>
/// <b>The scope is required</b>, so no search is the whole catalogue. <b>At most one of
/// <paramref name="TypedSearchText"/> and <paramref name="ExpandedSearchTerms"/> may be set.</b> They
/// are two different name-matching rules — every word against any term — so carrying both would mean a
/// caller set two and was answered by one of them without knowing which.
/// </remarks>
/// <param name="Scope">The category (and, for an accessory, the subcategory) to read.</param>
/// <param name="TypedSearchText">What a customer typed into a search box, or null when they typed nothing.</param>
/// <param name="ExpandedSearchTerms">The terms a search was expanded into, or null when it was not expanded.</param>
/// <param name="MaximumMonthlyAmount">The most a product may cost each month, or null for no ceiling.</param>
/// <param name="Take">How many ranked rows are returned; the scope is read in full before ranking.</param>
public sealed record SearchCatalogQuery(
    CatalogScope Scope,
    string? TypedSearchText = null,
    IReadOnlyList<string>? ExpandedSearchTerms = null,
    decimal? MaximumMonthlyAmount = null,
    int Take = CatalogPageLimits.Default);
