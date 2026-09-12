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
/// The order is the one thing here the catalog cannot answer, because it is how the products are
/// arranged on a screen rather than what they are; the wording comes from the catalog's own labels,
/// which the search matches against as well. Held as data rather than as a condition in the markup
/// so that the panel, the store and the picker cannot disagree about either.
/// </remarks>
public static class AccessoryGroups
{
    private static readonly CatalogSubCategory[] Order =
    [
        CatalogSubCategory.Monitor,
        CatalogSubCategory.Lamp,
        CatalogSubCategory.Plant,
        CatalogSubCategory.Coffee,
        CatalogSubCategory.Beanbag,
    ];

    /// <summary>The label above this subcategory's products.</summary>
    public static string LabelFor(CatalogSubCategory subCategory) => CatalogLabels.ForSubCategory(subCategory);

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

        foreach (var subCategory in Order)
        {
            var matching = products.Where(product => product.SubCategory == subCategory).ToArray();

            if (matching.Length > 0)
            {
                groups.Add(new CatalogGroup(subCategory, LabelFor(subCategory), matching));
            }
        }

        var unlabelled = products.Where(product => product.SubCategory is null).ToArray();

        return unlabelled.Length > 0
            ? [new CatalogGroup(null, string.Empty, unlabelled), .. groups]
            : groups;
    }
}
