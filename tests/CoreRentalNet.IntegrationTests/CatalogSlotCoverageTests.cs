using AwesomeAssertions;
using CoreRentalNet.Modules.Catalog.Infrastructure;
using CoreRentalNet.Modules.Workspace.Application.Rules;
using CoreRentalNet.Modules.Workspace.Domain;
using Xunit;

namespace CoreRentalNet.IntegrationTests;

/// <summary>
/// Proves the catalog-to-slot mapping stays total over the real file. If someone adds a
/// product with a new subcategory, this fails instead of the customer seeing a dead card.
/// </summary>
public sealed class CatalogSlotCoverageTests
{
    private static string ProductsJson => RepoRoot.Combine("src", "shared", "data", "products.json");

    [Fact] // SLOT-06
    public void Every_product_in_the_real_catalog_maps_to_exactly_one_slot()
    {
        var catalog = new ProductCatalog(ProductsJson, null);

        var mapped = catalog.All
            .Select(product => (product.Sku, Slot: CatalogSlotMapping.SlotFor(product.Category, product.SubCategory)))
            .ToArray();

        mapped.Should().HaveCount(catalog.All.Count);
        mapped.Should().OnlyHaveUniqueItems(entry => entry.Sku);
    }

    [Fact] // SLOT-06
    public void Every_slot_has_at_least_one_product_that_can_fill_it()
    {
        var catalog = new ProductCatalog(ProductsJson, null);

        var reachable = catalog.All
            .Select(product => CatalogSlotMapping.SlotFor(product.Category, product.SubCategory))
            .Distinct()
            .ToArray();

        reachable.Should().BeEquivalentTo(
            Enum.GetValues<SlotId>(),
            "a slot with no product behind it is a dashed placeholder that can never be filled");
    }

    [Fact] // SLOT-02
    public void The_catalog_is_large_enough_to_fill_a_workspace_and_then_some()
    {
        var catalog = new ProductCatalog(ProductsJson, null);

        catalog.All.Count.Should().BeGreaterThan(new SlotRuleProvider().TotalCapacity);
    }
}
