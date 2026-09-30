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
        ranker.TypedSearchTextItWasGiven.Should().BeNull("nothing typed is not a search");
        ranker.ExpandedSearchTermsItWasGiven.Should().BeNull("nothing was expanded either");
    }

    [Fact] // CAT-13
    public void What_was_typed_ranks_what_the_filters_allowed()
    {
        using var file = new TemporaryCatalogFile(SampleCatalog.SevenRows);
        var catalogue = Catalogue(file);
        var chairs = catalogue.ByCategory(CatalogCategory.Chair);
        var ranker = new StubProductNameSearchService { Matches = chairs };
        var handler = new SearchCatalogHandler(catalogue, ranker);

        var products = handler.Handle(new SearchCatalogQuery(CatalogCategory.Chair, TypedSearchText: "chair"));

        products.Should().Equal(chairs);
        ranker.TypedSearchTextItWasGiven.Should().Be("chair");
    }

    [Fact] // CAT-13 — the term cannot reach past the filters
    public void The_ranker_is_given_only_what_the_filters_allowed()
    {
        using var file = new TemporaryCatalogFile(SampleCatalog.SevenRows);
        var catalogue = Catalogue(file);
        var ranker = new StubProductNameSearchService();
        var handler = new SearchCatalogHandler(catalogue, ranker);

        handler.Handle(new SearchCatalogQuery(
            CatalogCategory.Accessory,
            CatalogSubCategory.Lamp,
            TypedSearchText: "chair"));

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

        handler.Handle(new SearchCatalogQuery(TypedSearchText: "chair")).Should().BeEmpty();
    }

    [Fact] // an expanded search ranks with the other rule, and hands the terms over whole
    public void Expanded_terms_are_ranked_by_the_alternatives_rule()
    {
        using var file = new TemporaryCatalogFile(SampleCatalog.SevenRows);
        var catalogue = Catalogue(file);
        var chairs = catalogue.ByCategory(CatalogCategory.Chair);
        var ranker = new StubProductNameSearchService { Matches = chairs };
        var handler = new SearchCatalogHandler(catalogue, ranker);

        string[] expandedSearchTerms = ["ergonomic chair", "task seating"];

        var products = handler.Handle(new SearchCatalogQuery(
            CatalogCategory.Chair,
            ExpandedSearchTerms: expandedSearchTerms));

        products.Should().Equal(chairs);
        ranker.ExpandedSearchTermsItWasGiven.Should().Equal(expandedSearchTerms);
        ranker.TypedSearchTextItWasGiven.Should().BeNull("the typed rule was not the one asked for");
    }

    [Fact] // a query that asks for both rules is refused rather than answered by one of them
    public void A_query_that_narrows_by_name_twice_is_refused()
    {
        using var file = new TemporaryCatalogFile(SampleCatalog.SevenRows);
        var catalogue = Catalogue(file);
        var handler = new SearchCatalogHandler(catalogue, new StubProductNameSearchService());

        var act = () => handler.Handle(new SearchCatalogQuery(
            TypedSearchText: "chair",
            ExpandedSearchTerms: ["chair"]));

        act.Should().Throw<ArgumentException>().WithParameterName("query");
    }

    [Fact] // the ceiling is the catalogue's to apply, and it is applied before anything ranks
    public void A_monthly_ceiling_narrows_before_the_ranker_sees_anything()
    {
        using var file = new TemporaryCatalogFile(SampleCatalog.SevenRows);
        var catalogue = Catalogue(file);

        const decimal belowTheOnlyChair = 399_999m;
        const decimal exactlyTheOnlyChair = 400_000m;

        var tooExpensive = new StubProductNameSearchService();
        new SearchCatalogHandler(catalogue, tooExpensive).Handle(new SearchCatalogQuery(
            CatalogCategory.Chair,
            TypedSearchText: "chair",
            MaximumMonthlyAmount: belowTheOnlyChair));

        tooExpensive.ProductsItWasGiven.Should().BeEmpty("no chair is affordable at that ceiling");

        var affordable = new StubProductNameSearchService();
        new SearchCatalogHandler(catalogue, affordable).Handle(new SearchCatalogQuery(
            CatalogCategory.Chair,
            TypedSearchText: "chair",
            MaximumMonthlyAmount: exactlyTheOnlyChair));

        // The same query one rupiah higher, so what removed the chair was the ceiling and not the search: the
        // ranker was reached both times, and only the eligible set differed.
        affordable.ProductsItWasGiven.Should().Equal(catalogue.Search(CatalogCategory.Chair, null, exactlyTheOnlyChair));
        affordable.ProductsItWasGiven.Should().HaveCount(1);
    }
}
