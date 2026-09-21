using AwesomeAssertions;
using CoreRentalNet.BuildingBlocks.Domain;
using CoreRentalNet.Modules.Catalog.Application.Contracts;
using CoreRentalNet.Modules.Discovery.Application;
using Xunit;

namespace CoreRentalNet.Modules.Discovery.UnitTests;

/// <summary>
/// The text a product is embedded from.
/// </summary>
/// <remarks>
/// This is the one place a change to the embedded fields is caught before it reaches an index. A vector built
/// from a different text is not comparable with the ones already stored, and nothing downstream would fail:
/// the search would simply rank differently, and the only symptom would be slightly worse answers. So the
/// text is pinned as a literal rather than described.
/// </remarks>
public sealed class EmbeddedTextTests
{
    private static ProductView Product(
        IReadOnlyList<string>? tags = null,
        IReadOnlyDictionary<string, string>? attributes = null,
        IReadOnlyList<string>? bestFor = null,
        IReadOnlyList<string>? notFor = null)
        => new(
            Sku: "DSKB08XN4JDR",
            Name: "HON Mod Desk Shell, 60 x 30 x 29, Mahogany",
            Category: CatalogCategory.Desk,
            SubCategory: null,
            MonthlyPrice: new Money(266000m, Currencies.Idr),
            Description: "A wide desk shell with a scratch-resistant laminate top.",
            Metadata: new CatalogMetadata(
                tags ?? ["desks", "workstations"],
                attributes ?? new Dictionary<string, string>
                {
                    ["brand"] = "HON",
                    ["color"] = "Mahogany",
                    ["material"] = "Metal",
                    ["priceidrpermonth"] = "266000",
                    ["rating"] = "5.0",
                },
                bestFor ?? ["general office work"],
                notFor ?? ["buyers who want no assembly"]),
            ImagePath: "images/desk.jpg",
            ImageAvailable: true,
            IsFeatured: false);

    [Fact] // SCR-03
    public void The_text_of_a_fully_populated_product_is_pinned()
    {
        // A literal, on purpose. Adding a field, dropping one, renaming a label or changing the separator
        // fails here, where the reason can be read, rather than in an index that has quietly gone stale.
        // A change to this string is a change to the composition, and the constant below has to move with it.
        const string Expected =
            "name: HON Mod Desk Shell, 60 x 30 x 29, Mahogany\n" +
            "description: A wide desk shell with a scratch-resistant laminate top.\n" +
            "tags: desks, workstations\n" +
            "bestFor: general office work\n" +
            "notFor: buyers who want no assembly\n" +
            "attribute brand: HON\n" +
            "attribute color: Mahogany\n" +
            "attribute material: Metal\n" +
            "attribute priceidrpermonth: 266000\n" +
            "attribute rating: 5.0";

        EmbeddedText.Of(Product()).Should().Be(Expected);
    }

    [Fact] // SCR-03
    public void The_composition_is_versioned_and_names_what_the_text_carries()
    {
        EmbeddedText.Composition.Should().Be("name+description+metadata/1");

        // The label and the text are two statements about the same thing, so the label is required to name
        // the parts the text actually has. Without this, a field could be added to the renderer while the
        // composition string went on describing the previous one - and the index would record the wrong name
        // for the vectors it holds.
        var text = EmbeddedText.Of(Product());

        text.Should().StartWith("name: ");
        text.Should().Contain("\ndescription: ");
        text.Should().Contain("\ntags: ");
        text.Should().Contain("\nattribute ");
    }

    [Fact] // SCR-03
    public void Every_attribute_key_and_value_reaches_the_text()
    {
        // The decision was to embed the whole metadata record, prices and counts included, rather than a
        // curated allow-list. This is the test that says so: a field left out of the renderer fails here.
        IReadOnlyDictionary<string, string> attributes = new Dictionary<string, string>
        {
            ["brand"] = "HON",
            ["rating"] = "5.0",
            ["ratingcount"] = "11",
            ["priceusd"] = "199.66",
            ["priceidrpermonth"] = "266000",
            ["categorypath"] = "Office Products > Office Furniture & Lighting > Desks",
            ["assemblyrequired"] = "Yes",
        };

        var text = EmbeddedText.Of(Product(attributes: attributes));

        foreach (var attribute in attributes)
        {
            text.Should().Contain($"attribute {attribute.Key}: {attribute.Value}");
        }
    }

    [Fact] // SCR-03
    public void Every_tag_and_every_best_for_and_not_for_reaches_the_text()
    {
        var text = EmbeddedText.Of(Product(
            tags: ["adjustable", "standing", "desks"],
            bestFor: ["focused work", "long sessions"],
            notFor: ["a small room"]));

        text.Should().Contain("tags: adjustable, standing, desks");
        text.Should().Contain("bestFor: focused work, long sessions");
        text.Should().Contain("notFor: a small room");
    }

    [Fact] // SCR-03
    public void The_order_the_attributes_arrive_in_does_not_change_the_text()
    {
        // The loader's dictionary promises no order, and an index that is not reproducible cannot be checked
        // against the hash recorded beside it.
        var one = EmbeddedText.Of(Product(attributes: new Dictionary<string, string>
        {
            ["brand"] = "HON",
            ["color"] = "Mahogany",
            ["rating"] = "5.0",
        }));

        var other = EmbeddedText.Of(Product(attributes: new Dictionary<string, string>
        {
            ["rating"] = "5.0",
            ["color"] = "Mahogany",
            ["brand"] = "HON",
        }));

        one.Should().Be(other);
    }

    [Fact] // SCR-03
    public void A_section_a_product_says_nothing_for_leaves_no_label_behind()
    {
        var text = EmbeddedText.Of(Product(notFor: [], bestFor: []));

        text.Should().NotContain("notFor:");
        text.Should().NotContain("bestFor:");
        text.Should().Contain("tags: desks, workstations");
    }
}
