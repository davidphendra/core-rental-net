namespace CoreRentalNet.Modules.Catalog.Domain;

/// <summary>Only accessories carry a subcategory; chairs and desks do not.</summary>
/// <remarks>
/// This is the module's internal vocabulary. Callers see <c>CatalogSubCategory</c> from
/// <c>Catalog.Application.Contracts</c> instead, which is what keeps them out of this assembly.
/// </remarks>
public enum ProductSubCategory
{
    Beanbag = 1,
    Coffee = 2,
    Lamp = 3,
    Monitor = 4,
    Plant = 5,
}
