using CoreRentalNet.BuildingBlocks.Domain;
using CoreRentalNet.Modules.Catalog.Application.Contracts;

namespace CoreRentalNet.Modules.Catalog.Application.Catalog;

/// <summary>A product as the UI needs it.</summary>
public sealed record ProductListItem(
    string Sku,
    string Name,
    CatalogCategory Category,
    CatalogSubCategory? SubCategory,
    Money MonthlyPrice,
    string Description,
    string ImagePath,
    bool ImageAvailable,
    bool IsFeatured);
