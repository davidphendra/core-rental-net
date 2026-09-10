namespace CoreRentalNet.Modules.Catalog.Application.Contracts;

/// <summary>
/// Catalog's public contract. Workspace and Rentals price everything through this and
/// never resolve an amount from anywhere else.
/// </summary>
public interface IDefineProductPrices
{
    /// <summary>Returns the price for a SKU, or null when the catalog has no such SKU.</summary>
    ProductPriceView? FindPrice(string sku);

    IReadOnlyList<ProductPriceView> AllPrices();
}
