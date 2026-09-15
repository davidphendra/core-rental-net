using AwesomeAssertions;
using CoreRentalNet.BuildingBlocks.Domain;
using CoreRentalNet.Host.Presentation;
using CoreRentalNet.Modules.Catalog.Application.Contracts;
using CoreRentalNet.Modules.Workspace.Application.Workspace.Rules;
using CoreRentalNet.Modules.Workspace.Domain;
using Xunit;

namespace CoreRentalNet.Host.Tests;

/// <summary>
/// The rule that turns a catalogue into the list a slot offers.
/// </summary>
public sealed class SlotCatalogTests
{
    [Fact] // SLOT-11
    public void Every_slot_asks_for_exactly_what_it_accepts()
    {
        // QueryFor is the inverse of the mapping the domain uses, written out by hand, so this is
        // what stops the two from drifting: whatever a slot asks for has to map back to that same
        // slot, or the picker would offer things the canvas cannot hold.
        foreach (var slot in Enum.GetValues<SlotId>())
        {
            var query = SlotCatalog.QueryFor(slot);

            CatalogSlotMapping.SlotFor(query.Category!.Value, query.SubCategory)
                .Should().Be(slot, $"{slot} asks for {query.Category}/{query.SubCategory}");
        }
    }

    [Fact] // SLOT-12
    public void An_accessory_slot_asks_for_its_own_subcategory_and_not_the_whole_category()
    {
        var lamp = SlotCatalog.QueryFor(SlotId.Lamp);

        lamp.Category.Should().Be(CatalogCategory.Accessory);
        lamp.SubCategory.Should().Be(CatalogSubCategory.Lamp);

        // And the desk, having no subcategory, asks for its category alone.
        var desk = SlotCatalog.QueryFor(SlotId.Desk);

        desk.Category.Should().Be(CatalogCategory.Desk);
        desk.SubCategory.Should().BeNull();
    }

    [Fact] // SLOT-13
    public void What_a_slot_asks_for_is_what_it_gets()
    {
        var lamps = new ProductView("LMP0001", "Pererenan Clip Light", CatalogCategory.Accessory,
            CatalogSubCategory.Lamp, new Money(150000m, Currencies.Idr), "A clip light.", "/images/vendored/one.png", true, false);
        var desks = new ProductView("DSK0001", "Canggu Bamboo", CatalogCategory.Desk,
            null, new Money(900000m, Currencies.Idr), "A desk.", "/images/vendored/two.png", true, false);
        var catalog = new[] { lamps, desks };

        SlotCatalog.ForSlot(catalog, SlotId.Lamp).Should().BeEquivalentTo([lamps]);
        SlotCatalog.ForSlot(catalog, SlotId.Desk).Should().BeEquivalentTo([desks]);
        SlotCatalog.ForSlot(catalog, SlotId.Chair).Should().BeEmpty();
    }
}
