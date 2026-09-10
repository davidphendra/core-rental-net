using AwesomeAssertions;
using CoreRentalNet.BuildingBlocks.Domain;
using CoreRentalNet.Modules.Catalog.Application.Contracts;
using CoreRentalNet.Modules.Workspace.Application.Catalog;
using CoreRentalNet.Modules.Workspace.Domain;
using Xunit;

namespace CoreRentalNet.Modules.Workspace.UnitTests;

public sealed class CatalogSlotMappingTests
{
    [Fact] // WS-05
    public void Every_catalog_vocabulary_pair_maps_to_its_slot()
    {
        CatalogSlotMapping.SlotFor(CatalogCategory.Chair, null).Should().Be(SlotId.Chair);
        CatalogSlotMapping.SlotFor(CatalogCategory.Desk, null).Should().Be(SlotId.Desk);
        CatalogSlotMapping.SlotFor(CatalogCategory.Accessory, CatalogSubCategory.Monitor).Should().Be(SlotId.Monitor);
        CatalogSlotMapping.SlotFor(CatalogCategory.Accessory, CatalogSubCategory.Lamp).Should().Be(SlotId.Lamp);
        CatalogSlotMapping.SlotFor(CatalogCategory.Accessory, CatalogSubCategory.Plant).Should().Be(SlotId.Plant);
        CatalogSlotMapping.SlotFor(CatalogCategory.Accessory, CatalogSubCategory.Coffee).Should().Be(SlotId.CoffeeStation);
        CatalogSlotMapping.SlotFor(CatalogCategory.Accessory, CatalogSubCategory.Beanbag).Should().Be(SlotId.RelaxZone);
    }

    [Fact] // WS-05
    public void Every_slot_is_reachable_from_exactly_one_catalog_pair()
    {
        // Chairs and desks carry no subcategory, so those two pairs are reached explicitly.
        var reachable = new List<SlotId>
        {
            CatalogSlotMapping.SlotFor(CatalogCategory.Chair, null),
            CatalogSlotMapping.SlotFor(CatalogCategory.Desk, null),
        };

        foreach (var category in Enum.GetValues<CatalogCategory>())
        {
            foreach (var subCategory in Enum.GetValues<CatalogSubCategory>())
            {
                try
                {
                    reachable.Add(CatalogSlotMapping.SlotFor(category, subCategory));
                }
                catch (DomainRuleViolationException)
                {
                    // Chairs and desks carry no subcategory, so those pairs are not products.
                }
            }
        }

        reachable.Distinct().Should().BeEquivalentTo(Enum.GetValues<SlotId>());
    }

    [Fact] // WS-06
    public void An_impossible_pair_is_rejected_rather_than_guessed()
    {
        var action = () => CatalogSlotMapping.SlotFor(CatalogCategory.Chair, CatalogSubCategory.Coffee);

        action.Should().Throw<DomainRuleViolationException>().WithMessage("*no workspace slot*");
    }
}
