using CoreRentalNet.Modules.Catalog.Application.Catalog;
using CoreRentalNet.Modules.Catalog.Domain;

namespace CoreRentalNet.Modules.Catalog.Application.Queries;

/// <summary>The products the Home page's featured cards show, driven by the popular badge.</summary>
public sealed record GetFeaturedProducts;

public sealed class GetFeaturedProductsHandler(IProductCatalog catalog)
{
    public IReadOnlyList<ProductListItem> Handle(GetFeaturedProducts query)
    {
        ArgumentNullException.ThrowIfNull(query);
        return catalog.Featured().Select(product => product.ToListItem()).ToArray();
    }
}
