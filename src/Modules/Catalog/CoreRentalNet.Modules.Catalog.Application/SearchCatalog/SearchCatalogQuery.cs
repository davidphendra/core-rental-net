using CoreRentalNet.Modules.Catalog.Application.Contracts;

namespace CoreRentalNet.Modules.Catalog.Application.SearchCatalog;

/// <summary>
/// Lists products, optionally narrowed by category, subcategory and what the customer typed. This is
/// the query behind the Builder's category tabs, its slot picker and the Store page. Results preserve
/// catalog order.
/// </summary>
public sealed record SearchCatalogQuery(
    CatalogCategory? Category = null,
    CatalogSubCategory? SubCategory = null,
    string? Search = null);
