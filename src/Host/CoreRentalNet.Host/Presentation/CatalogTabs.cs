using System.Collections.Immutable;
using CoreRentalNet.BuildingBlocks.Application;
using CoreRentalNet.Modules.Catalog.Application.Queries.SearchCatalog;
using CoreRentalNet.Modules.Catalog.Application.Contracts;

namespace CoreRentalNet.Host.Presentation;

/// <summary>The tabs' names, icons, categories and loading.</summary>
public static class CatalogTabs
{
    /// <summary>Immutable constant data, not global mutable state.</summary>
    public static ImmutableArray<CatalogTab> All { get; } =
    [
        CatalogTab.Desks,
        CatalogTab.Chairs,
        CatalogTab.Accessories,
    ];

    /// <summary>What a tab is called, from the label on the catalogService's own category.</summary>
    public static string LabelFor(CatalogTab tab) => CategoryFor(tab).Label();

    /// <summary>The icon the design gives each tab, from the glyph on the catalogService's own category.</summary>
    public static string GlyphFor(CatalogTab tab) => CategoryFor(tab).Glyph();

    /// <summary>The one category a tab shows.</summary>
    public static CatalogCategory CategoryFor(CatalogTab tab) => tab switch
    {
        CatalogTab.Desks => CatalogCategory.Desk,
        CatalogTab.Chairs => CatalogCategory.Chair,
        CatalogTab.Accessories => CatalogCategory.Accessory,
        _ => throw new ArgumentOutOfRangeException(nameof(tab), tab, "Unknown catalogService tab."),
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
    public static IReadOnlyList<ProductView> Load(
        CatalogTab tab,
        ISearchCatalogHandler handlerService,
        string? search = null)
    {
        ArgumentNullException.ThrowIfNull(handlerService);

        return handlerService.Handle(new SearchCatalogQuery(Category: CategoryFor(tab), Search: search));
    }
}
