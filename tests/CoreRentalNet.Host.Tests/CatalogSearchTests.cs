using AwesomeAssertions;
using CoreRentalNet.BuildingBlocks.Domain;
using CoreRentalNet.Host.Presentation;
using CoreRentalNet.Modules.Catalog.Application.Catalog;
using CoreRentalNet.Modules.Catalog.Application.Contracts;
using Xunit;

namespace CoreRentalNet.Host.Tests;

/// <summary>
/// The rule behind both search fields, which is the same rule applied to whatever each surface is
/// showing: the panel narrows its tab and the picker narrows its own products.
/// </summary>
public sealed class CatalogSearchTests
{
    private static readonly IReadOnlyList<ProductListItem> Catalog =
    [
        Product("Pererenan Clip Light", CatalogCategory.Accessory, CatalogSubCategory.Lamp),
        Product("Monstera Plant", CatalogCategory.Accessory, CatalogSubCategory.Plant),
        Product("Canggu Bamboo", CatalogCategory.Desk),
        Product("Seminyak Lounge", CatalogCategory.Chair),
    ];

    [Theory] // CAT-30
    [InlineData("pererenan", 1)] // the name
    [InlineData("PERERENAN", 1)] // whatever the case
    [InlineData("  clip  ", 1)] // and whatever the spacing
    [InlineData("light", 1)]
    [InlineData("lamp", 1)] // the kind, which no name in the catalog contains
    [InlineData("lamps", 1)] // singular or plural
    [InlineData("desk", 1)]
    [InlineData("chair", 1)]
    [InlineData("plant", 1)]
    [InlineData("accessories", 2)] // everything the accessory entry holds
    [InlineData("nothing", 0)]
    public void A_query_finds_products_by_name_or_by_the_kind_of_thing_they_are(string query, int expected)
        => CatalogSearch.Matching(Catalog, query).Should().HaveCount(expected);

    [Theory] // CAT-31
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void An_empty_query_leaves_the_list_alone(string? query)
        => CatalogSearch.Matching(Catalog, query).Should().BeEquivalentTo(Catalog);

    [Fact] // CAT-32
    public void A_search_never_reaches_outside_the_list_it_was_given()
    {
        // This is what keeps the panel's field and the picker's field apart: both are this rule over
        // what their own surface already held, and neither is ever handed the catalog.
        var desksOnly = Catalog.Where(product => product.Category == CatalogCategory.Desk).ToArray();

        CatalogSearch.Matching(desksOnly, "lamp").Should().BeEmpty();
    }

    private static ProductListItem Product(string name, CatalogCategory category, CatalogSubCategory? subCategory = null)
        => new($"SKU-{name[..3].ToUpperInvariant()}", name, category, subCategory, Money.Idr(100000m),
            "A description.", "/images/vendored/product.png", true, IsFeatured: false);
}
