using CoreRentalNet.Modules.Catalog.Application.Contracts;
using CoreRentalNet.Modules.Catalog.Application.Queries.SearchCatalog;

namespace CoreRentalNet.Modules.Catalog.Application.Queries.SearchSimilarityCatalog;

/// <summary>
/// Lists the catalogue by how near each product is to a sentence, optionally narrowed by category and
/// subcategory. Results are nearest first.
/// </summary>
/// <remarks>
/// Separate from <see cref="SearchCatalogQuery"/>, which carries a term matched against a product's name:
/// this one carries a sentence matched against a product's embedded text. The two answer different questions
/// and are reached by different endpoints, so they are different records rather than one with a flag.
/// </remarks>
/// <param name="Category">The category to narrow to, or null for all of them.</param>
/// <param name="SubCategory">The subcategory to narrow to, or null for all of them.</param>
/// <param name="Query">What the caller asked for, as a sentence.</param>
/// <param name="MaximumMonthlyAmount">The most a product may cost each month, or null for no ceiling.</param>
public sealed record SearchSimilarityCatalogQuery(
    CatalogCategory? Category,
    CatalogSubCategory? SubCategory,
    string Query,
    decimal? MaximumMonthlyAmount = null);
