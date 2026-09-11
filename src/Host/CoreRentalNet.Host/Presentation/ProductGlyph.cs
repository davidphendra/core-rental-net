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
        // The design sets these two in an icon font, as "light" and "local_florist": a bulb and a
        // flower, not the lamp shade and the potted plant that were here. The font is not shipped -
        // a font that may be missing leaves an empty box - so the shapes are drawn to match what
        // those two icons depict.
        "lamp" => "M12 3a6 6 0 0 0-6 6c0 2.2 1.2 3.6 2.4 4.7.4.4.6.9.6 1.3h6c0-.4.2-.9.6-1.3C16.8 12.6 18 11.2 18 9a6 6 0 0 0-6-6 M9.5 18h5 M10.5 21h3",
        "plant" => "M12 11.5a2 2 0 1 0 0-4 2 2 0 0 0 0 4 M12 7.5c0-2 1.4-3.5 3.2-3.5 0 2-1.4 3.5-3.2 3.5 M12 7.5c0-2-1.4-3.5-3.2-3.5 0 2 1.4 3.5 3.2 3.5 M14.5 9.5c1.7-1 3.7-.6 4.6 1-1.7 1-3.7.6-4.6-1 M9.5 9.5c-1.7-1-3.7-.6-4.6 1 1.7 1 3.7.6 4.6-1 M12 11.5V21",
        "plus" => "M12 5v14 M5 12h14",
        "keyboard" => "M3 7h18v10H3z M6 10h1 M10 10h1 M14 10h1 M18 10h1 M8 14h8",
        "beach_access" => "M12 3a9 9 0 0 0-9 9h18a9 9 0 0 0-9-9 M12 12v5a3 3 0 0 0 6 0",
        "coffee" => "M6 9h10v6a4 4 0 0 1-4 4h-2a4 4 0 0 1-4-4z M16 10h2a2 2 0 0 1 0 4h-2 M8 5c0-1 1-1 1-2 M12 5c0-1 1-1 1-2",
        _ => "M6 18c-2 0-3-2-2-4l3-7c1-2 3-3 5-3s4 1 5 3l3 7c1 2 0 4-2 4z M9 18v2 M15 18v2",
    };
}
