using CoreRentalNet.BuildingBlocks.Domain;
using CoreRentalNet.Modules.Catalog.Application.Contracts;
using CoreRentalNet.Modules.Workspace.Domain;

namespace CoreRentalNet.Modules.Workspace.Application.Catalog;

/// <summary>
/// Translates the catalog's vocabulary into this module's slot vocabulary.
/// </summary>
/// <remarks>
/// This is a total function over the catalog: a product maps to exactly one slot, so the
/// customer never picks a slot and a product can never land in the wrong one. A test walks
/// every product in the real catalog to prove the mapping stays total as the file changes.
/// </remarks>
public static class CatalogSlotMapping
{
    public static SlotId SlotFor(CatalogCategory category, CatalogSubCategory? subCategory)
        => (category, subCategory) switch
        {
            (CatalogCategory.Chair, null) => SlotId.Chair,
            (CatalogCategory.Desk, null) => SlotId.Desk,
            (CatalogCategory.Accessory, CatalogSubCategory.Monitor) => SlotId.Monitor,
            (CatalogCategory.Accessory, CatalogSubCategory.Lamp) => SlotId.Lamp,
            (CatalogCategory.Accessory, CatalogSubCategory.Plant) => SlotId.Plant,
            (CatalogCategory.Accessory, CatalogSubCategory.Coffee) => SlotId.CoffeeStation,
            (CatalogCategory.Accessory, CatalogSubCategory.Beanbag) => SlotId.RelaxZone,
            _ => throw new DomainRuleViolationException(
                $"No workspace slot accepts a {category} product with subcategory '{subCategory}'."),
        };
}
