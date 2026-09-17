using AwesomeAssertions;
using CoreRentalNet.BuildingBlocks.Domain;
using CoreRentalNet.Host.Presentation;
using CoreRentalNet.Modules.Catalog.Application.Contracts;
using Xunit;

namespace CoreRentalNet.Host.Tests;

/// <summary>
/// The order and the wording both the panel and the store render from, which is the point of it
/// being one definition: two surfaces showing the same catalog cannot disagree about what to call
/// a group or which comes first.
/// </summary>
public sealed class AccessoryGroupsTests
{
    [Fact] // CAT-33
    public void Groups_are_labelled_and_ordered_the_way_they_are_shown()
    {
        var groups = AccessoryGroups.Of([Plant, Coffee, Monitor, Beanbag, Lamp]);

        groups.Select(group => group.Label)
            .Should().BeEquivalentTo(
                ["Monitors", "Lamps", "Plants", "Coffee Machines", "Bean bags"],
                options => options.WithStrictOrdering());

        groups.Select(group => group.SubCategory).Should().OnlyContain(subCategory => subCategory != null);
    }

    [Fact] // CAT-34
    public void The_products_of_each_group_are_the_products_of_that_subcategory()
    {
        var groups = AccessoryGroups.Of([Plant, Monitor, Lamp, Plant]);

        groups.Single(group => group.SubCategory == CatalogSubCategory.Plant).Products.Should().HaveCount(2);
        groups.Single(group => group.SubCategory == CatalogSubCategory.Monitor).Products.Should().BeEquivalentTo([Monitor]);
        groups.Should().HaveCount(3, "a subcategory with nothing in it is not shown at all");
    }

    [Fact] // CAT-35
    public void Desks_and_chairs_come_back_as_one_group_with_nothing_above_them()
    {
        var groups = AccessoryGroups.Of([Desk, Chair, Lamp]);

        groups.Should().HaveCount(2);

        // First, so that a tab of plain things reads the same whether or not any accessory is in it,
        // and unlabelled, which is what tells the markup not to draw a heading.
        groups[0].SubCategory.Should().BeNull();
        groups[0].Label.Should().BeEmpty();
        groups[0].Products.Should().BeEquivalentTo([Desk, Chair]);

        groups[1].Label.Should().Be("Lamps");
    }

    [Fact] // CAT-36
    public void Every_subcategory_the_catalog_has_a_label_for_has_a_group()
    {
        // A subcategory with no label would appear in no group and vanish from the tab entirely.
        foreach (var subCategory in Enum.GetValues<CatalogSubCategory>())
        {
            AccessoryGroups.LabelFor(subCategory).Should().NotBeEmpty($"{subCategory} needs a label");
        }
    }

    private static ProductView Lamp => Product("LMP0001", "Pererenan Clip Light", CatalogSubCategory.Lamp);
    private static ProductView Plant => Product("PLT0001", "Monstera Plant", CatalogSubCategory.Plant);
    private static ProductView Monitor => Product("MON0001", "Nusa Dua Touch", CatalogSubCategory.Monitor);
    private static ProductView Coffee => Product("COF0001", "Berawa Espresso Duo", CatalogSubCategory.Coffee);
    private static ProductView Beanbag => Product("BEA0001", "Seminyak Floor Seat", CatalogSubCategory.Beanbag);
    private static ProductView Desk => Product("DSK0001", "Canggu Bamboo", null, CatalogCategory.Desk);
    private static ProductView Chair => Product("CHA0001", "Seminyak Lounge", null, CatalogCategory.Chair);

    private static ProductView Product(
        string sku,
        string name,
        CatalogSubCategory? subCategory,
        CatalogCategory category = CatalogCategory.Accessory)
        => new(sku, name, category, subCategory, new Money(100000m, Currencies.Idr), "A description.",
            new CatalogMetadata([], new Dictionary<string, string>(), [], []),
            "/images/vendored/product.png", true, false);
}
