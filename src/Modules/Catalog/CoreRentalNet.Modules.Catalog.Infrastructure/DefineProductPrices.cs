using CoreRentalNet.Modules.Catalog.Application.Catalog;
using CoreRentalNet.Modules.Catalog.Application.Contracts;
using CoreRentalNet.Modules.Catalog.Domain;

namespace CoreRentalNet.Modules.Catalog.Infrastructure;

/// <summary>Catalog's public price contract, served from the in-memory snapshot.</summary>
public sealed class DefineProductPrices(IProductCatalog catalog) : IDefineProductPrices
{
    public ProductPriceView? FindPrice(string sku)
    {
        if (!Sku.TryParse(sku, out var parsed))
        {
            return null;
        }

        return catalog.Find(parsed)?.ToPriceView();
    }

    public IReadOnlyList<ProductPriceView> AllPrices()
        => catalog.All.Select(product => product.ToPriceView()).ToArray();
}
