using CoreRentalNet.Modules.Catalog.Application.Contracts;

namespace CoreRentalNet.Modules.Catalog.Application.GetProductBySku;

/// <summary>One product by SKU, as a caller asks for it.</summary>
public interface IGetProductBySkuHandler
{
    /// <summary>Returns null for an unknown or malformed SKU rather than throwing.</summary>
    ProductView? Handle(GetProductBySku query);
}
