using CoreRentalNet.Modules.Catalog.Application.Catalog;
using CoreRentalNet.Modules.Catalog.Domain;

namespace CoreRentalNet.Modules.Catalog.Application.Queries;

public sealed record GetProductBySku(string Sku);

public sealed class GetProductBySkuHandler(IProductCatalog catalog)
{
    /// <summary>Returns null for an unknown or malformed SKU rather than throwing.</summary>
    public ProductListItem? Handle(GetProductBySku query)
    {
        ArgumentNullException.ThrowIfNull(query);

        return Sku.TryParse(query.Sku, out var sku) ? catalog.Find(sku)?.ToListItem() : null;
    }
}
