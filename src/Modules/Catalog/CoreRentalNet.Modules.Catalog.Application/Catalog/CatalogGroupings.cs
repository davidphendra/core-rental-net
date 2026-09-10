using CoreRentalNet.Modules.Catalog.Application.Contracts;

namespace CoreRentalNet.Modules.Catalog.Application.Catalog;

/// <summary>The two groupings the Builder's category navigation exposes.</summary>
public enum CatalogGrouping
{
    Accessories = 1,
    Extras = 2,
}

/// <summary>
/// Which subcategories each grouping contains. Held as data so the navigation can be
/// retuned without touching a conditional, and so the two groupings are provably disjoint.
/// </summary>
public static class CatalogGroupings
{
    private static readonly CatalogSubCategory[] Accessories =
    [
        CatalogSubCategory.Monitor,
        CatalogSubCategory.Lamp,
        CatalogSubCategory.Plant,
    ];

    private static readonly CatalogSubCategory[] Extras =
    [
        CatalogSubCategory.Coffee,
        CatalogSubCategory.Beanbag,
    ];

    public static IReadOnlyList<CatalogSubCategory> SubCategoriesFor(CatalogGrouping grouping) => grouping switch
    {
        CatalogGrouping.Accessories => Accessories,
        CatalogGrouping.Extras => Extras,
        _ => throw new ArgumentOutOfRangeException(nameof(grouping), grouping, "Unknown catalog grouping."),
    };

    public static IReadOnlyList<CatalogGrouping> All => [CatalogGrouping.Accessories, CatalogGrouping.Extras];
}
