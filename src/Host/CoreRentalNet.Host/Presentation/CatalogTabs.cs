using CoreRentalNet.Modules.Catalog.Application.Catalog;
using CoreRentalNet.Modules.Catalog.Application.Contracts;
using CoreRentalNet.Modules.Catalog.Application.Queries;

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

public static class CatalogTabs
{
    public static IReadOnlyList<CatalogTab> All { get; } =
    [
        CatalogTab.Desks,
        CatalogTab.Chairs,
        CatalogTab.Accessories,
    ];

    public static string LabelFor(CatalogTab tab) => CatalogLabels.ForCategory(CategoryFor(tab));

    /// <summary>The icon the design gives each entry.</summary>
    public static string GlyphFor(CatalogTab tab) => tab switch
    {
        CatalogTab.Desks => "desk",
        CatalogTab.Chairs => "chair",
        CatalogTab.Accessories => "keyboard",
        _ => throw new ArgumentOutOfRangeException(nameof(tab), tab, "Unknown catalog tab."),
    };

    /// <summary>The one category a tab shows.</summary>
    public static CatalogCategory CategoryFor(CatalogTab tab) => tab switch
    {
        CatalogTab.Desks => CatalogCategory.Desk,
        CatalogTab.Chairs => CatalogCategory.Chair,
        CatalogTab.Accessories => CatalogCategory.Accessory,
        _ => throw new ArgumentOutOfRangeException(nameof(tab), tab, "Unknown catalog tab."),
    };

    /// <summary>
    /// The tab a remembered name refers to, or desks when the name is missing or unknown.
    /// </summary>
    /// <remarks>
    /// The name arrives in a cookie, which is to say from the browser, so it is compared against the
    /// tab names rather than trusted. Comparing names rather than parsing numbers keeps a stray "3"
    /// from selecting the third tab. A browser still holding "Extras" from before the two accessory
    /// tabs were merged is sent to Accessories, which is where what it was looking at now lives.
    /// </remarks>
    public static CatalogTab FromName(string? name)
    {
        if (string.Equals(name, "Extras", StringComparison.Ordinal))
        {
            return CatalogTab.Accessories;
        }

        return Enum.GetNames<CatalogTab>().Contains(name, StringComparer.Ordinal)
            ? Enum.Parse<CatalogTab>(name!)
            : CatalogTab.Desks;
    }

    /// <summary>Everything a tab shows, in one query for one category.</summary>
    /// <param name="search">What the customer typed, or null for the whole tab.</param>
    public static IReadOnlyList<ProductListItem> Load(
        CatalogTab tab,
        GetCatalogPageHandler pageHandler,
        string? search = null)
    {
        ArgumentNullException.ThrowIfNull(pageHandler);

        return pageHandler.Handle(new GetCatalogPage(Category: CategoryFor(tab), Search: search));
    }
}
