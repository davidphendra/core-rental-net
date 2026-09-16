using CoreRentalNet.Modules.Catalog.Application.Contracts;

namespace CoreRentalNet.Modules.Catalog.Application.GetProductByFeature;

/// <summary>The featured products, as the home page asks for them.</summary>
public interface IGetFeaturedProductsHandler
{
    IReadOnlyList<ProductView> Handle(GetFeaturedProducts query);
}
