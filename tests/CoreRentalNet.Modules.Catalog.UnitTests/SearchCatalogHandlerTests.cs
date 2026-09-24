using AwesomeAssertions;
using CoreRentalNet.Modules.Catalog.Application.Contracts;
using CoreRentalNet.Modules.Catalog.Application.Queries.SearchCatalog;
using CoreRentalNet.Modules.Catalog.Infrastructure;
using Xunit;

namespace CoreRentalNet.Modules.Catalog.UnitTests;

/// <summary>
/// The handler's rule: the filters narrow, the name search ranks, and nothing typed is not nothing found.
/// </summary>
/// <remarks>
/// The ranking is stood in for, so what is asserted is the handler's own behaviour — above all the order of
/// those two steps, because a term that could widen what the filters excluded would be a term that reaches
/// products the caller was never allowed to see.
/// </remarks>
public sealed class SearchCatalogHandlerTests
{
    private static ProductCatalogService Catalogue(TemporaryCatalogFile file)
        => new(file.Path, null);

    [Fact] // CAT-14
    public void Nothing_typed_returns_what_the_filters_allowed_in_catalogue_order()
    {
        using var file = new TemporaryCatalogFile(SampleCatalog.SevenRows);
        var catalogue = Catalogue(file);
        var ranker = new StubProductNameSearchService();
        var handler = new SearchCatalogHandler(catalogue, ranker);

        var products = handler.Handle(new SearchCatalogQuery(CatalogCategory.Chair));

        products.Should().Equal(catalogue.Search(CatalogCategory.Chair, null));
        ranker.SearchTextItWasGiven.Should().BeNull("nothing typed is not a search");
    }

    [Fact] // CAT-13
    public void What_was_typed_ranks_what_the_filters_allowed()
    {
        using var file = new TemporaryCatalogFile(SampleCatalog.SevenRows);
        var catalogue = Catalogue(file);
        var chairs = catalogue.ByCategory(CatalogCategory.Chair);
        var ranker = new StubProductNameSearchService { Matches = chairs };
        var handler = new SearchCatalogHandler(catalogue, ranker);

        var products = handler.Handle(new SearchCatalogQuery(CatalogCategory.Chair, Search: "chair"));

        products.Should().Equal(chairs);
        ranker.SearchTextItWasGiven.Should().Be("chair");
    }

    [Fact] // CAT-13 — the term cannot reach past the filters
    public void The_ranker_is_given_only_what_the_filters_allowed()
    {
        using var file = new TemporaryCatalogFile(SampleCatalog.SevenRows);
        var catalogue = Catalogue(file);
        var ranker = new StubProductNameSearchService();
        var handler = new SearchCatalogHandler(catalogue, ranker);

        handler.Handle(new SearchCatalogQuery(CatalogCategory.Accessory, CatalogSubCategory.Lamp, "chair"));

        ranker.ProductsItWasGiven.Should().Equal(
            catalogue.Search(CatalogCategory.Accessory, CatalogSubCategory.Lamp));
        ranker.ProductsItWasGiven.Should().OnlyContain(product => product.SubCategory == CatalogSubCategory.Lamp);
    }

    [Fact]
    public void A_product_the_ranker_did_not_match_is_left_out()
    {
        using var file = new TemporaryCatalogFile(SampleCatalog.SevenRows);
        var catalogue = Catalogue(file);
        var handler = new SearchCatalogHandler(catalogue, new StubProductNameSearchService());

        handler.Handle(new SearchCatalogQuery(Search: "chair")).Should().BeEmpty();
    }
}
