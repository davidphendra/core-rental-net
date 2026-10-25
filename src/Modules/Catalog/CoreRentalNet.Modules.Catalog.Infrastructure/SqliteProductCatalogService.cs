using CoreRentalNet.BuildingBlocks.Domain;
using CoreRentalNet.Modules.Catalog.Application.Contracts;
using CoreRentalNet.Modules.Catalog.Domain;
using CoreRentalNet.Modules.Catalog.Infrastructure.Persistence;
using CoreRentalNet.Modules.Catalog.Infrastructure.ProductImage;
using Microsoft.EntityFrameworkCore;
using ProductImageResolver = CoreRentalNet.Modules.Catalog.Infrastructure.ProductImage.ProductImage;

namespace CoreRentalNet.Modules.Catalog.Infrastructure;

/// <summary>
/// The catalogue's adapter over its SQLite store: scoped, bounded, read-only.
/// </summary>
/// <remarks>
/// <para>
/// <b>No operation reads the whole catalogue.</b> Every query narrows by the scope's category (and, for an
/// accessory, its subcategory) and takes at most <see cref="CatalogPageLimits.Maximum"/> rows, so the size of
/// an answer is bounded by the scope rather than by the store.
/// </para>
/// <para>
/// <b>The ceiling is applied after the bounded scope is read, not in SQL.</b> SQLite stores a decimal as text,
/// where a <c>&lt;=</c> is a string comparison and would keep the wrong rows; the scope is small, so filtering
/// it in memory is both correct and cheap.
/// </para>
/// <para>
/// <b>The web root is what lets an image be resolved here rather than on every page.</b> The store keeps the
/// raw image text; whether it is available is a fact about this deployment, not about the row.
/// </para>
/// </remarks>
public sealed class SqliteProductCatalogService(CatalogContext context, string? webRootPath) : IProductCatalogService
{
    private readonly IProductImage _images = new ProductImageResolver(webRootPath);

    /// <inheritdoc />
    public async Task<ProductDto?> FindAsync(string sku, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(sku);

        var row = await context.Products
            .AsNoTracking()
            .FirstOrDefaultAsync(product => product.Sku == sku, cancellationToken)
            .ConfigureAwait(false);

        return row is null ? null : Map(row);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ProductDto>> FeaturedAsync(CancellationToken cancellationToken)
    {
        var rows = await context.Products
            .AsNoTracking()
            .Where(product => product.IsFeatured)
            .OrderBy(product => product.SortOrder)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return [.. rows.Select(Map)];
    }

    /// <inheritdoc />
    public async Task<CatalogPage> SearchPageAsync(
        CatalogScope scope,
        decimal? maximumMonthlyAmount,
        int skip,
        int take,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(scope);

        take = Math.Clamp(take, 1, CatalogPageLimits.Maximum);
        var subCategory = scope.SubCategory is { } requestedSubCategory
            ? (int?)ProductDtoMapper.ToProductSubCategory(requestedSubCategory)
            : null;

        var rows = await context.Products
            .AsNoTracking()
            .Where(product => product.Category == (int)scope.Category
                              && (subCategory == null || product.SubCategory == subCategory))
            .OrderBy(product => product.SortOrder)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var eligible = rows
            .Where(row => maximumMonthlyAmount is null || row.Amount <= maximumMonthlyAmount)
            .ToArray();

        var page = eligible
            .Skip(skip)
            .Take(take)
            .Select(Map)
            .ToArray();

        return new CatalogPage(page, eligible.Length);
    }

    /// <inheritdoc />
    public async Task<string> CurrencyAsync(CancellationToken cancellationToken)
    {
        var currency = await context.Products
            .AsNoTracking()
            .Select(product => product.Currency)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        return currency ?? Currencies.Idr;
    }

    private ProductDto Map(CatalogProduct row)
    {
        var resolved = _images.Resolve(row.Sku, row.Image);

        return new ProductDto(
            row.Sku,
            row.Name,
            (CatalogCategory)row.Category,
            row.SubCategory is { } subCategory
                ? ProductDtoMapper.ToCatalogSubCategory((ProductSubCategory)subCategory)
                : null,
            new Money(row.Amount, row.Currency),
            row.Description,
            resolved.Path,
            resolved.Available,
            row.IsFeatured,
            ProductFactsMapper.From(row));
    }
}
