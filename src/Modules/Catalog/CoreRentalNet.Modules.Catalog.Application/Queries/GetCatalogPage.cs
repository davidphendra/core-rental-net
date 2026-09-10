using CoreRentalNet.Modules.Catalog.Application.Catalog;
using CoreRentalNet.Modules.Catalog.Application.Contracts;
using CoreRentalNet.Modules.Catalog.Domain;

namespace CoreRentalNet.Modules.Catalog.Application.Queries;

/// <summary>
/// Lists products, optionally narrowed by category and/or subcategory. This is the query
/// behind the Builder's category tabs and the Store page. Results preserve catalog order.
/// </summary>
public sealed record GetCatalogPage(CatalogCategory? Category = null, CatalogSubCategory? SubCategory = null);

public sealed class GetCatalogPageHandler(IProductCatalog catalog)
{
    public IReadOnlyList<ProductListItem> Handle(GetCatalogPage query)
    {
        ArgumentNullException.ThrowIfNull(query);

        IEnumerable<Product> source = catalog.All;

        if (query.Category is { } category)
        {
            source = source.Where(product => product.Category == category.ToDomain());
        }

        if (query.SubCategory is { } subCategory)
        {
            source = source.Where(product => product.SubCategory == subCategory.ToDomain());
        }

        return source.Select(product => product.ToListItem()).ToArray();
    }
}
