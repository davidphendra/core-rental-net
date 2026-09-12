using CoreRentalNet.Modules.Catalog.Application.Catalog;
using CoreRentalNet.Modules.Catalog.Application.Contracts;

namespace CoreRentalNet.Host.Presentation;

/// <summary>One subcategory's products under the label they are shown beneath.</summary>
public sealed record CatalogGroup(
    CatalogSubCategory? SubCategory,
    string Label,
    IReadOnlyList<ProductListItem> Products);

/// <summary>
/// The accessory subcategories in the order they are displayed, with the label above each.
/// </summary>
/// <remarks>
/// Held as data rather than as a condition in the markup, and shared, so the panel, the store and
/// the picker cannot disagree about the wording or the order. Before the two accessory entries
/// became one there was nothing to tell apart; now a single tab carries five kinds of thing.
/// </remarks>
public static class AccessoryGroups
{
    private static readonly (CatalogSubCategory SubCategory, string Label)[] Order =
    [
        (CatalogSubCategory.Monitor, "Monitors"),
        (CatalogSubCategory.Lamp, "Lamps"),
        (CatalogSubCategory.Plant, "Plants"),
        (CatalogSubCategory.Coffee, "Coffee Machines"),
        (CatalogSubCategory.Beanbag, "Bean bags"),
    ];

    /// <summary>The label above this subcategory's products, or nothing when it has none.</summary>
    public static string LabelFor(CatalogSubCategory subCategory)
    {
        foreach (var (candidate, label) in Order)
        {
            if (candidate == subCategory)
            {
                return label;
            }
        }

        return string.Empty;
    }

    /// <summary>
    /// Splits products into their subcategories, in that order, leaving out the empty ones.
    /// </summary>
    /// <remarks>
    /// Products with no subcategory - desks and chairs - come back as one group with no label, so a
    /// caller can render groups unconditionally and never has to ask whether it should be grouping:
    /// a heading appears when there is a label, and only accessories have one.
    /// </remarks>
    public static IReadOnlyList<CatalogGroup> Of(IReadOnlyList<ProductListItem> products)
    {
        ArgumentNullException.ThrowIfNull(products);

        var groups = new List<CatalogGroup>();

        foreach (var (subCategory, label) in Order)
        {
            var matching = products.Where(product => product.SubCategory == subCategory).ToArray();

            if (matching.Length > 0)
            {
                groups.Add(new CatalogGroup(subCategory, label, matching));
            }
        }

        var unlabelled = products.Where(product => product.SubCategory is null).ToArray();

        return unlabelled.Length > 0
            ? [new CatalogGroup(null, string.Empty, unlabelled), .. groups]
            : groups;
    }
}
