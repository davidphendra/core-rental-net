using AwesomeAssertions;
using CoreRentalNet.BuildingBlocks.Domain;
using CoreRentalNet.Host.Presentation;
using CoreRentalNet.Modules.Catalog.Application.Catalog;
using CoreRentalNet.Modules.Catalog.Application.Contracts;
using Xunit;

namespace CoreRentalNet.Host.Tests;

/// <summary>
/// The browsing state the panel and the store each used to keep for themselves.
/// </summary>
/// <remarks>
/// The browser is asked through a query, so the tests answer with a list of their own and assert the
/// rules rather than the catalog: which tab is current, what happens to the search when the tab
/// changes, and that the catalog is asked with both.
/// </remarks>
public sealed class CatalogBrowserTests
{
    [Fact] // STORE-08
    public void It_opens_on_desks_with_no_search()
    {
        var browser = new CatalogBrowser((_, _) => []);

        browser.Tab.Should().Be(CatalogTab.Desks);
        browser.Query.Should().BeEmpty();
        browser.Items.Should().BeEmpty();
    }

    [Fact] // STORE-08
    public void Choosing_a_category_empties_the_search()
    {
        var browser = new CatalogBrowser((_, _) => []);

        browser.Search("lamp");
        browser.SelectTab(CatalogTab.Chairs);

        browser.Tab.Should().Be(CatalogTab.Chairs);
        browser.Query.Should().BeEmpty(
            "a search that matched nothing in the last tab must not hide this one's products");
    }

    [Fact] // STORE-08
    public void The_catalog_is_asked_with_the_current_tab_and_search()
    {
        CatalogTab? askedTab = null;
        string? askedQuery = null;

        var browser = new CatalogBrowser((tab, query) =>
        {
            askedTab = tab;
            askedQuery = query;
            return [];
        });

        browser.SelectTab(CatalogTab.Accessories);
        browser.Search("lamp");

        askedTab.Should().Be(CatalogTab.Accessories);
        askedQuery.Should().Be("lamp");
    }

    [Fact] // STORE-08
    public void What_the_catalog_answers_is_what_the_surface_shows()
    {
        var item = new ProductListItem(
            "LMP0001",
            "Pererenan Clip Light",
            CatalogCategory.Accessory,
            CatalogSubCategory.Lamp,
            Money.Idr(180000m),
            "A lamp.",
            "/images/lamp.svg",
            true,
            false);

        var browser = new CatalogBrowser((_, _) => [item]);

        browser.Reload();

        browser.Items.Should().ContainSingle().Which.Should().Be(item);
    }

    [Fact] // STORE-08
    public void A_missing_search_is_the_empty_search()
    {
        string? asked = "not asked";

        var browser = new CatalogBrowser((_, query) =>
        {
            asked = query;
            return [];
        });

        browser.Search(null);

        browser.Query.Should().BeEmpty();
        asked.Should().BeEmpty();
    }
}
