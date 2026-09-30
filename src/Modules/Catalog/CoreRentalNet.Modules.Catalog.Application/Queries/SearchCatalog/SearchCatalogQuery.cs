using CoreRentalNet.Modules.Catalog.Application.Contracts;

namespace CoreRentalNet.Modules.Catalog.Application.Queries.SearchCatalog;

/// <summary>
/// Lists products, optionally narrowed by category, subcategory and a monthly ceiling. This is
/// the query behind the Builder's category tabs, its slot picker and the Store page. Results preserve
/// catalog order.
/// </summary>
/// <remarks>
/// <b>At most one of <paramref name="TypedSearchText"/> and <paramref name="ExpandedSearchTerms"/> may be
/// set.</b> They are two different name-matching rules — every word against any term — so carrying both would
/// mean a caller set two and was answered by one of them without knowing which.
/// <see cref="SearchCatalogHandler"/> refuses that rather than choosing, because the rule it would be
/// choosing belongs to the caller.
/// </remarks>
/// <param name="Category">The category to narrow to, or null for all of them.</param>
/// <param name="SubCategory">The accessory subcategory to narrow to, or null for all of them.</param>
/// <param name="TypedSearchText">What a customer typed into a search box, or null when they typed nothing.</param>
/// <param name="ExpandedSearchTerms">The terms a search was expanded into, or null when it was not expanded.</param>
/// <param name="MaximumMonthlyAmount">The most a product may cost each month, or null for no ceiling.</param>
public sealed record SearchCatalogQuery(
    CatalogCategory? Category = null,
    CatalogSubCategory? SubCategory = null,
    string? TypedSearchText = null,
    IReadOnlyList<string>? ExpandedSearchTerms = null,
    decimal? MaximumMonthlyAmount = null);
