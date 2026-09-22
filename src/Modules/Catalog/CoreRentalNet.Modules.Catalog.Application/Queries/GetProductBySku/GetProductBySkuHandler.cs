using CoreRentalNet.Modules.Catalog.Application.Contracts;

namespace CoreRentalNet.Modules.Catalog.Application.Queries.GetProductBySku;

/// <summary>Answers <see cref="IGetProductBySkuHandler"/> through the catalogue service.</summary>
public sealed class GetProductBySkuHandler(IProductCatalogService catalogService) : IGetProductBySkuHandler
{
    /// <summary>Returns null for an unknown or malformed SKU rather than throwing.</summary>
    public ProductView? Handle(GetProductBySku query)
    {
        ArgumentNullException.ThrowIfNull(query);

        return catalogService.Find(query.Sku);
    }
}
