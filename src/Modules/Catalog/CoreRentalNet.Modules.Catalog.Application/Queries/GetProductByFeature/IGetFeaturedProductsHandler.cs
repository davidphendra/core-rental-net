using CoreRentalNet.Modules.Catalog.Application.Contracts;

namespace CoreRentalNet.Modules.Catalog.Application.Queries.GetProductByFeature;

/// <summary>The featured products, as the home page asks for them.</summary>
public interface IGetFeaturedProductsHandler
{
    IReadOnlyList<ProductDto> Handle(GetFeaturedProducts query);
}
