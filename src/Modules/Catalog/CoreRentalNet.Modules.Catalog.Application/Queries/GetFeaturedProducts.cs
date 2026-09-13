using CoreRentalNet.Modules.Catalog.Application.Catalog;
using CoreRentalNet.Modules.Catalog.Domain;

namespace CoreRentalNet.Modules.Catalog.Application.Queries;

/// <summary>The products the Home page's featured cards show, driven by the popular badge.</summary>
public sealed record GetFeaturedProducts;

/// <summary>The featured products, as the home page asks for them.</summary>
public interface IGetFeaturedProducts
{
    IReadOnlyList<ProductListItem> Handle(GetFeaturedProducts query);
}

public sealed class GetFeaturedProductsHandler(IProductCatalog catalog) : IGetFeaturedProducts
{
    public IReadOnlyList<ProductListItem> Handle(GetFeaturedProducts query)
    {
        ArgumentNullException.ThrowIfNull(query);
        return catalog.Featured().Select(product => product.ToListItem()).ToArray();
    }
}
