using System.ComponentModel;

namespace CoreRentalNet.Modules.Catalog.Application.Contracts;

/// <summary>
/// The subcategory vocabulary this module publishes to its callers. Only accessories carry one.
/// </summary>
/// <remarks>
/// Deliberately separate from <c>Catalog.Domain.ProductSubCategory</c>, for the reason given on
/// <see cref="CatalogCategory"/>. "Coffee Machines" and "Bean bags" are why the label is data: an
/// identifier is for code, a label is for people.
/// </remarks>
public enum CatalogSubCategory
{
    [Description("Monitors")]
    Monitor = 1,

    [Description("Lamps")]
    Lamp = 2,

    [Description("Plants")]
    Plant = 3,

    [Description("Coffee Machines")]
    Coffee = 4,

    [Description("Bean bags")]
    Beanbag = 5,
}
