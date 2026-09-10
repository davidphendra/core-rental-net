using CoreRentalNet.BuildingBlocks.Domain;

namespace CoreRentalNet.Modules.Catalog.Application.Contracts;

/// <summary>
/// The catalog's public view of a product: what it is called, where it belongs, and what
/// it costs per month. The SKU is a plain string so that consumers do not depend on a
/// Domain type.
/// </summary>
public sealed record ProductPriceView(
    string Sku,
    string Name,
    CatalogCategory Category,
    CatalogSubCategory? SubCategory,
    Money MonthlyPrice);
