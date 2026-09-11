using CoreRentalNet.Modules.Catalog.Application.Contracts;
using CoreRentalNet.Modules.Workspace.Domain;

namespace CoreRentalNet.Host.Presentation;

/// <summary>The line-art glyph shown when a product has no image file.</summary>
public static class ProductGlyph
{
    public static string ForProduct(CatalogCategory category, CatalogSubCategory? subCategory)
        => category switch
        {
            CatalogCategory.Chair => "chair",
            CatalogCategory.Desk => "desk",
            CatalogCategory.Accessory => subCategory switch
            {
                CatalogSubCategory.Monitor => "monitor",
                CatalogSubCategory.Lamp => "lamp",
                CatalogSubCategory.Plant => "plant",
                CatalogSubCategory.Coffee => "coffee",
                CatalogSubCategory.Beanbag => "beanbag",
                _ => "beanbag",
            },
            _ => "beanbag",
        };

    public static string ForSlot(SlotId slot) => slot switch
    {
        SlotId.Desk => "desk",
        SlotId.Chair => "chair",
        SlotId.Monitor => "monitor",
        SlotId.Lamp => "lamp",
        SlotId.Plant => "plant",
        SlotId.CoffeeStation => "coffee",
        SlotId.RelaxZone => "beanbag",
        _ => "beanbag",
    };

    public static string PathFor(string glyph) => glyph switch
    {
        "chair" => "M7 4h10v8H7z M9 12v8 M15 12v8 M5 8H3 M21 8h-2",
        "desk" => "M3 9h18v2H3z M5 11v8 M19 11v8",
        "monitor" => "M4 5h16v10H4z M9 19h6 M12 15v4",
        "lamp" => "M9 4h6l2 7H7z M12 11v7 M8 20h8",
        "plant" => "M8 13h8l-1 7H9z M12 13c0-4 3-6 6-6 0 4-3 6-6 6 M12 13c0-3-2-5-5-5 0 3 2 5 5 5",
        "plus" => "M12 5v14 M5 12h14",
        "keyboard" => "M3 7h18v10H3z M6 10h1 M10 10h1 M14 10h1 M18 10h1 M8 14h8",
        "beach_access" => "M12 3a9 9 0 0 0-9 9h18a9 9 0 0 0-9-9 M12 12v5a3 3 0 0 0 6 0",
        "coffee" => "M6 9h10v6a4 4 0 0 1-4 4h-2a4 4 0 0 1-4-4z M16 10h2a2 2 0 0 1 0 4h-2 M8 5c0-1 1-1 1-2 M12 5c0-1 1-1 1-2",
        _ => "M6 18c-2 0-3-2-2-4l3-7c1-2 3-3 5-3s4 1 5 3l3 7c1 2 0 4-2 4z M9 18v2 M15 18v2",
    };
}
