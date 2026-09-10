using AwesomeAssertions;
using CoreRentalNet.Modules.Catalog.Application.Contracts;
using CoreRentalNet.Modules.Catalog.Application.Catalog;
using CoreRentalNet.Modules.Catalog.Infrastructure.Loading;
using CoreRentalNet.Modules.Workspace.Application.Catalog;
using CoreRentalNet.Modules.Workspace.Domain;
using Xunit;

namespace CoreRentalNet.IntegrationTests;

/// <summary>
/// Proves the catalog-to-slot mapping stays total over the real file. If someone adds a
/// product with a new subcategory, this fails instead of the customer seeing a dead card.
/// </summary>
public sealed class CatalogSlotCoverageTests
{
    [Fact] // SLOT-06
    public void Every_product_in_the_real_catalog_maps_to_exactly_one_slot()
    {
        var catalog = CatalogLoader.LoadFromFile(RepoRoot.Combine("src", "shared", "data", "products.json"));

        var mapped = new List<(string Sku, SlotId Slot)>();

        foreach (var product in catalog.All)
        {
            var slot = CatalogSlotMapping.SlotFor(product.Category.ToContract(), product.SubCategory.ToContract());
            mapped.Add((product.Sku.Value, slot));
        }

        mapped.Should().HaveCount(catalog.All.Count);
        mapped.Should().OnlyHaveUniqueItems(entry => entry.Sku);
    }

    [Fact] // SLOT-06
    public void Every_slot_has_at_least_one_product_that_can_fill_it()
    {
        var catalog = CatalogLoader.LoadFromFile(RepoRoot.Combine("src", "shared", "data", "products.json"));

        var reachable = catalog.All
            .Select(product => CatalogSlotMapping.SlotFor(product.Category.ToContract(), product.SubCategory.ToContract()))
            .Distinct()
            .ToArray();

        reachable.Should().BeEquivalentTo(
            Enum.GetValues<SlotId>(),
            "a slot with no product behind it is a dashed placeholder that can never be filled");
    }

    [Fact] // SLOT-02
    public void The_catalog_is_large_enough_to_fill_a_workspace_and_then_some()
    {
        var catalog = CatalogLoader.LoadFromFile(RepoRoot.Combine("src", "shared", "data", "products.json"));

        catalog.All.Count.Should().BeGreaterThan(SlotRules.TotalCapacity);
    }
}
