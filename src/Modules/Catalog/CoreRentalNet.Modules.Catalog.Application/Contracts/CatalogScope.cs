namespace CoreRentalNet.Modules.Catalog.Application.Contracts;

/// <summary>
/// Which slice of the catalogue a query reads. Carries the values; it validates nothing.
/// </summary>
/// <remarks>
/// A category is always present, so a query can never be the whole catalogue. A subcategory is present
/// only for an accessory; chairs and desks have none, and the shape rule for that lives in one place
/// (the loader), not here.
/// </remarks>
public sealed record CatalogScope(CatalogCategory Category, CatalogSubCategory? SubCategory);
