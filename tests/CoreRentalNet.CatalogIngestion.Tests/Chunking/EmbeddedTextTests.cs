using AwesomeAssertions;
using CoreRentalNet.BuildingBlocks.Domain;
using CoreRentalNet.CatalogIngestion.Chunking;
using CoreRentalNet.Modules.Catalog.Application.Contracts;
using Xunit;

namespace CoreRentalNet.CatalogIngestion.Tests.Chunking;

/// <summary>
/// The text a product is chunked from.
/// </summary>
/// <remarks>
/// The rendering is not cosmetic: it decides the sentence boundaries, and therefore the chunks. It also has
/// to be reproducible, or two runs over one unchanged catalogue would produce different vectors and nothing
/// would say why.
/// </remarks>
public sealed class EmbeddedTextTests
{
    private static ProductView Product(IReadOnlyList<string>? notFor = null)
        => new(
            Sku: "DSK0001",
            Name: "Desk DSK0001",
            Category: CatalogCategory.Desk,
            SubCategory: null,
            MonthlyPrice: new Money(800000m, Currencies.Idr),
            Description: "A height-adjustable desk.",
            Metadata: new CatalogMetadata(
                ["desk", "standing"],
                new Dictionary<string, string> { ["type"] = "sit-stand", ["colour"] = "oak" },
                ["all-day use"],
                notFor ?? ["outdoor use"]),
            ImagePath: "/images/desk.svg",
            ImageAvailable: true,
            IsFeatured: false);

    [Fact]
    public void The_text_is_the_name_the_description_and_every_metadata_value()
    {
        var text = EmbeddedText.Of(Product());

        text.Should().Contain("name: Desk DSK0001");
        text.Should().Contain("description: A height-adjustable desk.");
        text.Should().Contain("tags: desk, standing");
        text.Should().Contain("bestFor: all-day use");
        text.Should().Contain("notFor: outdoor use");
        text.Should().Contain("attribute colour: oak");
        text.Should().Contain("attribute type: sit-stand");
    }

    [Fact]
    public void Attributes_are_ordered_by_key_so_the_text_does_not_depend_on_dictionary_order()
    {
        var text = EmbeddedText.Of(Product());

        text.IndexOf("attribute colour", StringComparison.Ordinal)
            .Should().BeLessThan(text.IndexOf("attribute type", StringComparison.Ordinal));
    }

    [Fact]
    public void A_section_the_product_has_nothing_for_is_left_out_entirely()
    {
        // An empty label is a line the model would read as meaning something, which is why it is omitted
        // rather than written blank.
        EmbeddedText.Of(Product(notFor: [])).Should().NotContain("notFor:");
    }

    [Fact]
    public void The_same_product_renders_the_same_text_twice()
    {
        EmbeddedText.Of(Product()).Should().Be(EmbeddedText.Of(Product()));
    }
}
