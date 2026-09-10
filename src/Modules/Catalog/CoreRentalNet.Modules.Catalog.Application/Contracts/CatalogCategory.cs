namespace CoreRentalNet.Modules.Catalog.Application.Contracts;

/// <summary>
/// The category vocabulary exposed to other modules. Deliberately separate from
/// <c>Catalog.Domain.ProductCategory</c> so that no other module has to reference this
/// module's Domain, which the architecture tests enforce.
/// </summary>
public enum CatalogCategory
{
    Chair = 1,
    Desk = 2,
    Accessory = 3,
}
