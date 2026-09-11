using CoreRentalNet.Modules.Catalog.Application.Catalog;
using CoreRentalNet.Modules.Catalog.Application.Contracts;
using CoreRentalNet.Modules.Catalog.Application.Queries;

namespace CoreRentalNet.Host.Presentation;

/// <summary>The four entries in the Builder's selection panel.</summary>
public enum CatalogTab
{
    Chairs = 1,
    Desks = 2,
    Accessories = 3,
    Extras = 4,
}

public static class CatalogTabs
{
    public static IReadOnlyList<CatalogTab> All { get; } =
    [
        CatalogTab.Chairs,
        CatalogTab.Desks,
        CatalogTab.Accessories,
        CatalogTab.Extras,
    ];

    public static string LabelFor(CatalogTab tab) => tab switch
    {
        CatalogTab.Chairs => "Chairs",
        CatalogTab.Desks => "Desks",
        CatalogTab.Accessories => "Accessories",
        CatalogTab.Extras => "Extras",
        _ => throw new ArgumentOutOfRangeException(nameof(tab), tab, "Unknown catalog tab."),
    };

    /// <summary>The icon the design gives each entry.</summary>
    public static string GlyphFor(CatalogTab tab) => tab switch
    {
        CatalogTab.Chairs => "chair",
        CatalogTab.Desks => "desk",
        CatalogTab.Accessories => "keyboard",
        CatalogTab.Extras => "beach_access",
        _ => throw new ArgumentOutOfRangeException(nameof(tab), tab, "Unknown catalog tab."),
    };

    /// <summary>
    /// Chairs and Desks are whole categories; Accessories and Extras are the two groupings that
    /// split the Accessory category by subcategory.
    /// </summary>
    public static IReadOnlyList<ProductListItem> Load(
        CatalogTab tab,
        GetCatalogPageHandler pageHandler,
        GetCatalogGroupHandler groupHandler)
        => tab switch
        {
            CatalogTab.Chairs => pageHandler.Handle(new GetCatalogPage(Category: CatalogCategory.Chair)),
            CatalogTab.Desks => pageHandler.Handle(new GetCatalogPage(Category: CatalogCategory.Desk)),
            CatalogTab.Accessories => groupHandler.Handle(new GetCatalogGroup(CatalogGrouping.Accessories)),
            CatalogTab.Extras => groupHandler.Handle(new GetCatalogGroup(CatalogGrouping.Extras)),
            _ => throw new ArgumentOutOfRangeException(nameof(tab), tab, "Unknown catalog tab."),
        };
}
