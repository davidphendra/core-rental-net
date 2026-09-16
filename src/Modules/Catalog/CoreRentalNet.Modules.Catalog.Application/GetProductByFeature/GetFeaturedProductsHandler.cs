using CoreRentalNet.Modules.Catalog.Application.Contracts;

namespace CoreRentalNet.Modules.Catalog.Application.GetProductByFeature;

/// <summary>Answers <see cref="IGetFeaturedProductsHandler"/> through the catalogue service.</summary>
public sealed class GetFeaturedProductsHandler(IProductCatalog catalog) : IGetFeaturedProductsHandler
{
    public IReadOnlyList<ProductView> Handle(GetFeaturedProducts query)
    {
        ArgumentNullException.ThrowIfNull(query);

        return catalog.Featured();
    }
}
