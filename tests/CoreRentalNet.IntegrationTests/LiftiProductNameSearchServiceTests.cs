using AwesomeAssertions;
using CoreRentalNet.Modules.Catalog.Application.Contracts;
using CoreRentalNet.Modules.Catalog.Infrastructure;
using CoreRentalNet.Modules.Discovery.Infrastructure.NameSearch;
using Xunit;

namespace CoreRentalNet.IntegrationTests;

/// <summary>
/// The catalogue's name search, ranked: what a word matches, and in what order.
/// </summary>
/// <remarks>
/// The scoring is the package's Okapi BM25 and the tokenisation is the package's, so what is asserted here is
/// the index's shape and the contract around it — that a name is what is searched, that a whole word is
/// answered before a word that merely begins with it, that a product the caller may not see is never returned,
/// and that two runs give the same answer.
/// </remarks>
public sealed class LiftiProductNameSearchServiceTests
{
    private static string ProductsJson => RepoRoot.Combine("src", "shared", "data", "products.json");

    private static (ProductCatalogService Catalogue, LiftiProductNameSearchService NameSearch) Build()
    {
        var catalogue = new ProductCatalogService(ProductsJson, null);

        return (catalogue, new LiftiProductNameSearchService(catalogue));
    }

    [Fact] // CAT-12
    public async Task A_word_matches_a_product_by_its_name()
    {
        var (catalogue, nameSearch) = Build();

        var matches = await nameSearch.FindBestMatchesAsync(catalogue.All, "Kahuna");

        matches.Should().NotBeEmpty();
        matches.Should().OnlyContain(product =>
            product.Name.Contains("Kahuna", StringComparison.OrdinalIgnoreCase));
    }

    [Fact] // a whole word is answered before a word that merely begins with it
    public async Task A_whole_word_is_answered_before_a_word_that_begins_with_it()
    {
        var (catalogue, nameSearch) = Build();

        // "Honey" begins with "hon", and the products this word is about are the HON desks. The lamp is not
        // hidden — the search box cannot afford to hide a word-start match — but it is answered last, so a
        // caller asking for two rows gets the two desks.
        var matches = await nameSearch.FindBestMatchesAsync(catalogue.All, "hon");

        matches.Take(2).Should().OnlyContain(product =>
            product.Name.Contains("HON ", StringComparison.OrdinalIgnoreCase));

        matches.Should().Contain(product =>
            product.Name.Contains("Honey", StringComparison.OrdinalIgnoreCase));
    }

    [Fact] // the panel and the picker look on every keystroke
    public async Task A_word_that_is_not_finished_still_matches()
    {
        var (catalogue, nameSearch) = Build();

        var matches = await nameSearch.FindBestMatchesAsync(catalogue.All, "Serta");

        matches.Should().Contain(product => product.Name.Contains("SertaPedic", StringComparison.Ordinal));
    }

    [Fact] // what a customer typed can only ever be a term, never query syntax
    public async Task A_wildcard_in_what_was_typed_is_not_a_wildcard()
    {
        var (catalogue, nameSearch) = Build();

        var act = () => nameSearch.FindBestMatchesAsync(catalogue.All, "100%");

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task A_word_that_matches_nothing_returns_nothing()
    {
        var (catalogue, nameSearch) = Build();

        (await nameSearch.FindBestMatchesAsync(catalogue.All, "submarine periscope")).Should().BeEmpty();
    }

    [Fact] // CAT-14
    public async Task Nothing_typed_returns_everything_in_catalogue_order()
    {
        var (catalogue, nameSearch) = Build();

        (await nameSearch.FindBestMatchesAsync(catalogue.All, "   ")).Should().Equal(catalogue.All);
    }

    [Fact] // CAT-13 — the caller's set is the boundary
    public async Task A_product_the_caller_may_not_see_is_never_returned()
    {
        var (catalogue, nameSearch) = Build();

        var desksOnly = catalogue.ByCategory(CatalogCategory.Desk);

        (await nameSearch.FindBestMatchesAsync(desksOnly, "Kahuna"))
            .Should().BeEmpty("the chair is not among the products the caller was given");
    }

    [Fact]
    public async Task The_same_search_twice_gives_the_same_answer()
    {
        var (catalogue, nameSearch) = Build();

        (await nameSearch.FindBestMatchesAsync(catalogue.All, "chair"))
            .Should().Equal(await nameSearch.FindBestMatchesAsync(catalogue.All, "chair"));
    }
}
