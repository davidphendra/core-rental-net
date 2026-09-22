using AwesomeAssertions;
using CoreRentalNet.BuildingBlocks.Application;
using CoreRentalNet.Host.Presentation;
using Xunit;

namespace CoreRentalNet.Host.Tests;

/// <summary>The tabs' words and icons, which come from the catalogService's own vocabulary.</summary>
public sealed class CatalogTabsTests
{
    [Fact]
    public void A_tab_is_named_by_its_category_label()
    {
        CatalogTabs.LabelFor(CatalogTab.Desks).Should().Be("Desks");
        CatalogTabs.LabelFor(CatalogTab.Chairs).Should().Be("Chairs");
        CatalogTabs.LabelFor(CatalogTab.Accessories).Should().Be("Accessories");
    }

    [Fact]
    public void A_tab_draws_the_glyph_the_catalog_gives_its_category()
    {
        foreach (var tab in CatalogTabs.All)
        {
            CatalogTabs.GlyphFor(tab).Should().Be(CatalogTabs.CategoryFor(tab).Glyph());
        }
    }

    [Fact]
    public void Every_tab_glyph_is_one_the_path_table_knows()
    {
        // A name the path table does not know draws its default shape, which is exactly the silent
        // wrong icon a mistyped glyph attribute would produce.
        var unknown = ProductGlyph.PathFor("no such glyph");

        foreach (var tab in CatalogTabs.All)
        {
            ProductGlyph.PathFor(CatalogTabs.GlyphFor(tab)).Should().NotBe(unknown, $"the {tab} tab draws its glyph");
        }
    }
}
