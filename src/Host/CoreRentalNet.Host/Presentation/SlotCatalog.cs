using CoreRentalNet.Modules.Catalog.Application.Queries.SearchCatalog;
using CoreRentalNet.Modules.Catalog.Application.Contracts;
using CoreRentalNet.Modules.Workspace.Application.Rules;
using CoreRentalNet.Modules.Workspace.Domain;

namespace CoreRentalNet.Host.Presentation;

/// <summary>Answers "what could go in this slot?" using the same total mapping the domain uses.</summary>
public static class SlotCatalog
{
    public static IReadOnlyList<ProductView> ForSlot(IEnumerable<ProductView> products, SlotId slot)
        => products
            .Where(product => CatalogSlotMapping.SlotFor(product.Category, product.SubCategory) == slot)
            .ToArray();

    /// <summary>
    /// The one query that answers <see cref="ForSlot"/>, which is the inverse of the same mapping.
    /// </summary>
    /// <remarks>
    /// A clicked slot asks the catalogService for its own kind of thing and nothing else: the desk asks for
    /// desks, and the lamp asks for the lamp subcategory - six lamps rather than the whole accessory
    /// category. Before this, opening any box loaded every category in the catalogService and filtered the
    /// result down, which is four queries to show six lamps.
    /// </remarks>
    public static SearchCatalogQuery QueryFor(SlotId slot) => slot switch
    {
        SlotId.Desk => new SearchCatalogQuery(Category: CatalogCategory.Desk),
        SlotId.Chair => new SearchCatalogQuery(Category: CatalogCategory.Chair),
        SlotId.Monitor => Accessories(CatalogSubCategory.Monitor),
        SlotId.Lamp => Accessories(CatalogSubCategory.Lamp),
        SlotId.Plant => Accessories(CatalogSubCategory.Plant),
        SlotId.CoffeeStation => Accessories(CatalogSubCategory.Coffee),
        SlotId.RelaxZone => Accessories(CatalogSubCategory.Beanbag),
        _ => throw new ArgumentOutOfRangeException(nameof(slot), slot, "No catalogService query answers for this slot."),
    };

    private static SearchCatalogQuery Accessories(CatalogSubCategory subCategory)
        => new(Category: CatalogCategory.Accessory, SubCategory: subCategory);
}
