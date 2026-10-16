namespace CoreRentalNet.Modules.Catalog.Application.Contracts;

/// <summary>
/// The catalogue read port: how this module's application and the other modules read the catalogue.
/// </summary>
/// <remarks>
/// <para>
/// <b>There is deliberately no write side and no unscoped read.</b> The catalogue is read-only, and every
/// query is scoped by a category (and, for an accessory, a subcategory), so no caller can ask for the whole
/// catalogue and no query has to load one.
/// </para>
/// <para>
/// <b>The operations are asynchronous because the store is a database, not a snapshot in memory.</b> A page
/// is bounded by <see cref="CatalogPageLimits"/>, and the total travels with it so a caller can tell a
/// complete answer from a truncated one.
/// </para>
/// </remarks>
public interface IProductCatalogService
{
    /// <summary>One product by SKU, ignoring letter case, or null when the catalogue has none.</summary>
    Task<ProductDto?> FindAsync(string sku, CancellationToken cancellationToken);

    /// <summary>The products carrying the popular badge.</summary>
    Task<IReadOnlyList<ProductDto>> FeaturedAsync(CancellationToken cancellationToken);

    /// <summary>
    /// One page of the products in a scope, in catalogue order, optionally under a monthly ceiling.
    /// </summary>
    /// <param name="scope">The category (and, for an accessory, the subcategory) to read.</param>
    /// <param name="maximumMonthlyAmount">The most a product may cost each month, or null for no ceiling.</param>
    /// <param name="skip">How many rows of the scope to pass over.</param>
    /// <param name="take">How many rows to return; bounded by <see cref="CatalogPageLimits"/>.</param>
    /// <param name="cancellationToken">The caller's cancellation token.</param>
    Task<CatalogPage> SearchPageAsync(
        CatalogScope scope,
        decimal? maximumMonthlyAmount,
        int skip,
        int take,
        CancellationToken cancellationToken);

    /// <summary>The currency every price is stated in, read from the catalogue rather than its rows.</summary>
    Task<string> CurrencyAsync(CancellationToken cancellationToken);
}
