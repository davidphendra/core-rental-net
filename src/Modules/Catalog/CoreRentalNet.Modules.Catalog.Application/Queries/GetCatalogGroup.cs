using CoreRentalNet.Modules.Catalog.Application.Catalog;
using CoreRentalNet.Modules.Catalog.Domain;

namespace CoreRentalNet.Modules.Catalog.Application.Queries;

/// <summary>
/// Lists one navigation grouping. Note this is not the same as filtering by category: the
/// Accessories and Extras groupings both draw from the Accessory category, split by
/// subcategory.
/// </summary>
public sealed record GetCatalogGroup(CatalogGrouping Grouping);

public sealed class GetCatalogGroupHandler(IProductCatalog catalog)
{
    public IReadOnlyList<ProductListItem> Handle(GetCatalogGroup query)
    {
        ArgumentNullException.ThrowIfNull(query);

        var wanted = CatalogGroupings
            .SubCategoriesFor(query.Grouping)
            .Select(subCategory => subCategory.ToDomain())
            .ToHashSet();

        return catalog.All
            .Where(product => product.SubCategory.HasValue && wanted.Contains(product.SubCategory.GetValueOrDefault()))
            .Select(product => product.ToListItem())
            .ToArray();
    }
}
