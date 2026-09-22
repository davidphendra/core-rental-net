using AwesomeAssertions;
using CoreRentalNet.Modules.Catalog.Infrastructure;
using CoreRentalNet.Modules.Workspace.Application.Rules;
using CoreRentalNet.Modules.Workspace.Domain;
using Xunit;

namespace CoreRentalNet.IntegrationTests;

/// <summary>
/// Proves the catalogService-to-slot mapping stays total over the real file. If someone adds a
/// product with a new subcategory, this fails instead of the customer seeing a dead card.
/// </summary>
public sealed class CatalogSlotCoverageTests
{
    private static string ProductsJson => RepoRoot.Combine("src", "shared", "data", "products.json");

    [Fact] // SLOT-06
    public void Every_product_in_the_real_catalog_maps_to_exactly_one_slot()
    {
        var catalog = new ProductCatalogService(ProductsJson, null);

        var mapped = catalog.All
            .Select(product => (product.Sku, Slot: CatalogSlotMapping.SlotFor(product.Category, product.SubCategory)))
            .ToArray();

        mapped.Should().HaveCount(catalog.All.Count);
        mapped.Should().OnlyHaveUniqueItems(entry => entry.Sku);
    }

    [Fact] // SLOT-06
    public void Every_slot_has_at_least_one_product_that_can_fill_it()
    {
        var catalog = new ProductCatalogService(ProductsJson, null);

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
        var catalog = new ProductCatalogService(ProductsJson, null);

        catalog.All.Count.Should().BeGreaterThan(new SlotRuleProvider().TotalCapacity);
    }

    [Fact] // SCR-07
    public void Two_catalog_buckets_never_share_a_slot()
    {
        // THE GUARD e06 RESTS ON, AND IT IS NOT THE SAME RULE AS THE TWO ABOVE. Totality says every product
        // reaches a slot, and the test above says every slot is reachable. Neither notices a SECOND subcategory
        // mapping to a slot that already has one - and that single addition would make the catalogue's own
        // vocabulary a FINER partition than the slots. Discovery takes the best two products in each
        // (category, subCategory) bucket, so a nine-bucket catalogue would hand the run more than fourteen
        // products, and "two per slot" would stop being what the shortlist is.
        var catalog = new ProductCatalogService(ProductsJson, null);

        var buckets = catalog.All
            .GroupBy(product => (product.Category, product.SubCategory))
            .Select(bucket => (Bucket: bucket.Key, Slot: CatalogSlotMapping.SlotFor(bucket.Key.Category, bucket.Key.SubCategory)))
            .ToArray();

        buckets.Should().HaveCountGreaterThan(
            1,
            "with one bucket the uniqueness check below would pass without proving anything");
        buckets.Select(bucket => bucket.Slot).Should().OnlyHaveUniqueItems(
            "two buckets mapping to one slot makes the catalogue vocabulary finer than the slot vocabulary, "
            + "and a per-bucket shortlist would then stop covering each slot once");
    }

    [Fact] // SCR-07
    public void The_catalog_buckets_and_the_slots_are_the_same_partition()
    {
        // Stated rather than inferred from the two tests either side of it, because it is the whole claim e06's
        // design rests on: the catalogue's own vocabulary is used for the buckets, with no mapping of its own
        // and no mirrored slot type, ONLY because the two partitions are the same. If that stops being true the
        // shortcut is no longer available and the buckets have to be expressed some other way.
        var catalog = new ProductCatalogService(ProductsJson, null);

        var buckets = catalog.All.Select(product => (product.Category, product.SubCategory)).Distinct().ToArray();
        var slots = Enum.GetValues<SlotId>();

        buckets.Should().HaveCount(slots.Length);
        buckets
            .Select(bucket => CatalogSlotMapping.SlotFor(bucket.Category, bucket.SubCategory))
            .Should().BeEquivalentTo(slots);
    }
}
