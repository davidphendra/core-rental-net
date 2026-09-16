using AwesomeAssertions;
using CoreRentalNet.BuildingBlocks.Application;
using CoreRentalNet.Modules.Catalog.Application.Contracts;
using Xunit;

namespace CoreRentalNet.Modules.Catalog.UnitTests;

/// <summary>The glyph each category is drawn with.</summary>
/// <remarks>
/// The tabs, the store's pills and the chips all draw the category's own glyph, and the path table
/// falls back to a default shape for a name it does not know - so a member left bare would draw the
/// wrong icon without anything failing.
/// </remarks>
public sealed class CatalogGlyphTests
{
    [Fact]
    public void Every_category_has_a_glyph_of_its_own()
    {
        foreach (var category in Enum.GetValues<CatalogCategory>())
        {
            category.Glyph().Should().NotBeNullOrWhiteSpace();
            category.Glyph().Should().NotBe(category.ToString(), "a glyph is not the member's own name");
        }
    }

    [Fact]
    public void The_glyphs_are_the_ones_the_design_names()
    {
        CatalogCategory.Chair.Glyph().Should().Be("chair");
        CatalogCategory.Desk.Glyph().Should().Be("desk");
        CatalogCategory.Accessory.Glyph().Should().Be("keyboard");
    }
}
