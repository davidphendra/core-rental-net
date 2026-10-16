using CoreRentalNet.Modules.Catalog.Application.Contracts;

namespace CoreRentalNet.Modules.Catalog.Application.Queries.SearchSimilarityCatalog;

/// <summary>
/// Lists the catalogue by how near each product is to a sentence, in one scope. Results are nearest first.
/// </summary>
/// <remarks>
/// Separate from <see cref="SearchCatalogQuery"/>, which carries a term matched against a product's name:
/// this one carries a sentence matched against a product's embedded text. The two answer different questions
/// and are reached by different endpoints, so they are different records rather than one with a flag.
/// </remarks>
/// <param name="Scope">The category (and, for an accessory, the subcategory) to read.</param>
/// <param name="Query">What the caller asked for, as a sentence.</param>
/// <param name="MaximumMonthlyAmount">The most a product may cost each month, or null for no ceiling.</param>
/// <param name="Take">How many rows the scope supplies before ranking; bounded by the port.</param>
public sealed record SearchSimilarityCatalogQuery(
    CatalogScope Scope,
    string Query,
    decimal? MaximumMonthlyAmount = null,
    int Take = CatalogPageLimits.Default);
