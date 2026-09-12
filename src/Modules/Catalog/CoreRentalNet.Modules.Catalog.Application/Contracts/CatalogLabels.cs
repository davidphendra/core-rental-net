namespace CoreRentalNet.Modules.Catalog.Application.Contracts;

/// <summary>
/// What each category and subcategory is called, in one place.
/// </summary>
/// <remarks>
/// The panel's tabs, the store's pills, the group headings and the search itself all name the same
/// things, and a search has to know the name it is matching against - so the words live beside the
/// enums they describe rather than in the screens that happen to show them. "Coffee Machines" and
/// "Bean bags" are why this is data rather than the enum's own name: an identifier is for code, a
/// label is for people.
/// </remarks>
public static class CatalogLabels
{
    public static string ForCategory(CatalogCategory category) => category switch
    {
        CatalogCategory.Chair => "Chairs",
        CatalogCategory.Desk => "Desks",
        CatalogCategory.Accessory => "Accessories",
        _ => throw new ArgumentOutOfRangeException(nameof(category), category, "Unknown catalog category."),
    };

    public static string ForSubCategory(CatalogSubCategory subCategory) => subCategory switch
    {
        CatalogSubCategory.Monitor => "Monitors",
        CatalogSubCategory.Lamp => "Lamps",
        CatalogSubCategory.Plant => "Plants",
        CatalogSubCategory.Coffee => "Coffee Machines",
        CatalogSubCategory.Beanbag => "Bean bags",
        _ => throw new ArgumentOutOfRangeException(nameof(subCategory), subCategory, "Unknown catalog subcategory."),
    };
}
