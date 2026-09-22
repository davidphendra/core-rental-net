using CoreRentalNet.Modules.Catalog.Application.Contracts;

namespace CoreRentalNet.Modules.Catalog.Application.Queries.GetProductByFeature;

/// <summary>Answers <see cref="IGetFeaturedProductsHandler"/> through the catalogue service.</summary>
public sealed class GetFeaturedProductsHandler(IProductCatalogService catalogService) : IGetFeaturedProductsHandler
{
    public IReadOnlyList<ProductView> Handle(GetFeaturedProducts query)
    {
        ArgumentNullException.ThrowIfNull(query);

        return catalogService.Featured();
    }
}
