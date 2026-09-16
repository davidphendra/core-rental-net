namespace CoreRentalNet.Host.Presentation;

/// <summary>The three entries in the Builder's selection panel and the store's pills.</summary>
/// <remarks>
/// Accessories used to be two entries, Accessories and Extras, split by subcategory. They are one
/// entry now, which is why this maps onto the catalog's own three categories instead of carrying a
/// grouping of its own: a tab that showed part of a category was a navigation idea, and there is
/// only one accessory category to navigate.
/// </remarks>
public enum CatalogTab
{
    Desks = 1,
    Chairs = 2,
    Accessories = 3,
}
