using AgentFoundry.WorkspaceSuggestions.Catalogue;
using AgentFoundry.WorkspaceSuggestions.Vocabularies;

namespace AgentFoundry.WorkspaceSuggestions.Selection;

/// <summary>
/// Which catalogue products belong to which slot.
/// </summary>
/// <remarks>
/// The map is asymmetric, and the asymmetry is a measured property of the catalogue rather than a
/// choice made here: desks and chairs carry a <c>category</c> and no subcategory, while the five
/// accessory slots carry a <c>subcategory</c> under the accessory category. A map that filtered a
/// subcategory called "desk" would return nothing at all for the two mandatory slots - measured, that
/// request answers with an empty list - so the two halves are written differently on purpose.
/// </remarks>
internal static class SlotCatalogue
{
    public static IReadOnlyList<CatalogueItem> For(CataloguePage page, string slot)
    {
        ArgumentNullException.ThrowIfNull(page);

        return slot switch
        {
            Slots.Desk => ByCategory(page, "desk"),
            Slots.Chair => ByCategory(page, "chair"),
            Slots.Monitor => BySubCategory(page, "monitor"),
            Slots.Lamp => BySubCategory(page, "lamp"),
            Slots.Plant => BySubCategory(page, "plant"),
            Slots.CoffeeStation => BySubCategory(page, "coffee"),
            Slots.RelaxZone => BySubCategory(page, "beanbag"),
            _ => [],
        };
    }

    private static IReadOnlyList<CatalogueItem> ByCategory(CataloguePage page, string category)
        => [.. page.Value.Where(item => string.Equals(item.Category, category, StringComparison.OrdinalIgnoreCase))];

    private static IReadOnlyList<CatalogueItem> BySubCategory(CataloguePage page, string subCategory)
        => [.. page.Value.Where(item => string.Equals(item.SubCategory, subCategory, StringComparison.OrdinalIgnoreCase))];
}
