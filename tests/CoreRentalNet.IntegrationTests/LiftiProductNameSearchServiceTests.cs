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
/// <para>
/// <b>Both name rules are asserted here, because the difference between them is the point.</b> What a customer
/// types must require every word — a half-typed box narrows towards the one thing they mean — while the terms
/// a search was expanded into must be alternatives, or one word the catalogue does not use would empty an
/// answer that had already decided what was wanted.
/// </para>
/// <para>
/// <b>Two fields are indexed and the name is boosted, so a match is on a name or on a description.</b> That is
/// deliberate and measured: a product's characteristics are in its description, and a need whose distinguishing
/// words are only there — a monitor that is 4K, a chair with lumbar support — was unreachable by any term before
/// the description was indexed. The name stays worth more, so a name match still outranks prose.
/// </para>
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

        var matches = await nameSearch.FindBestMatchesForTypedSearchWordsAsync(catalogue.All, "Kahuna");

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
        var matches = await nameSearch.FindBestMatchesForTypedSearchWordsAsync(catalogue.All, "hon");

        matches.Take(2).Should().OnlyContain(product =>
            product.Name.Contains("HON ", StringComparison.OrdinalIgnoreCase));

        matches.Should().Contain(product =>
            product.Name.Contains("Honey", StringComparison.OrdinalIgnoreCase));
    }

    [Fact] // the panel and the picker look on every keystroke
    public async Task A_word_that_is_not_finished_still_matches()
    {
        var (catalogue, nameSearch) = Build();

        var matches = await nameSearch.FindBestMatchesForTypedSearchWordsAsync(catalogue.All, "Serta");

        matches.Should().Contain(product => product.Name.Contains("SertaPedic", StringComparison.Ordinal));
    }

    [Fact] // what a customer typed can only ever be a term, never query syntax
    public async Task A_wildcard_in_what_was_typed_is_not_a_wildcard()
    {
        var (catalogue, nameSearch) = Build();

        var act = () => nameSearch.FindBestMatchesForTypedSearchWordsAsync(catalogue.All, "100%");

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task A_word_that_matches_nothing_returns_nothing()
    {
        var (catalogue, nameSearch) = Build();

        (await nameSearch.FindBestMatchesForTypedSearchWordsAsync(catalogue.All, "submarine periscope")).Should().BeEmpty();
    }

    [Fact] // CAT-14
    public async Task Nothing_typed_returns_everything_in_catalogue_order()
    {
        var (catalogue, nameSearch) = Build();

        (await nameSearch.FindBestMatchesForTypedSearchWordsAsync(catalogue.All, "   ")).Should().Equal(catalogue.All);
    }

    [Fact] // CAT-13 — the caller's set is the boundary
    public async Task A_product_the_caller_may_not_see_is_never_returned()
    {
        var (catalogue, nameSearch) = Build();

        var desksOnly = catalogue.ByCategory(CatalogCategory.Desk);

        (await nameSearch.FindBestMatchesForTypedSearchWordsAsync(desksOnly, "Kahuna"))
            .Should().BeEmpty("the chair is not among the products the caller was given");
    }

    [Fact]
    public async Task The_same_search_twice_gives_the_same_answer()
    {
        var (catalogue, nameSearch) = Build();

        (await nameSearch.FindBestMatchesForTypedSearchWordsAsync(catalogue.All, "chair"))
            .Should().Equal(await nameSearch.FindBestMatchesForTypedSearchWordsAsync(catalogue.All, "chair"));
    }

    // --- the expanded search: a term is a phrase, and the terms are alternatives -----------------------

    [Fact]
    public async Task A_term_is_a_phrase_and_every_word_of_it_must_appear()
    {
        var (catalogue, nameSearch) = Build();

        (await nameSearch.FindBestMatchesForExpandedSearchTermsAsync(catalogue.All, ["HON Mod"]))
            .Should().NotBeEmpty()
            .And.OnlyContain(product =>
                product.Name.Contains("HON", StringComparison.OrdinalIgnoreCase)
                && product.Name.Contains("Mod", StringComparison.OrdinalIgnoreCase));

        (await nameSearch.FindBestMatchesForExpandedSearchTermsAsync(catalogue.All, ["HON submarine"]))
            .Should().BeEmpty("one word the catalogue does not use makes the whole term match nothing");
    }

    [Fact]
    public async Task Terms_are_alternatives_so_one_that_matches_nothing_does_not_empty_the_answer()
    {
        var (catalogue, nameSearch) = Build();

        // Both rules over the same material, and this is the defect the change exists for. A customer's need is
        // a sentence, and as one search every word of it must appear — in a name or in a description — which no
        // product satisfies. As alternatives, the terms an expansion produces find the seating.
        const string NeedAsTheCustomerWroteIt = "my back hurts after sitting all day";

        (await nameSearch.FindBestMatchesForTypedSearchWordsAsync(catalogue.All, NeedAsTheCustomerWroteIt))
            .Should().BeEmpty("a sentence is not a word a catalogue prints");

        var alternatives = await nameSearch.FindBestMatchesForExpandedSearchTermsAsync(
            catalogue.All, ["ergonomic chair", "office chair"]);

        alternatives.Should().NotBeEmpty("the terms an expansion produces are what finds it");
        alternatives.Should().Contain(product => product.Category == CatalogCategory.Chair);

        // A term can now match a product outside the category it was meant for, because prose mentions other
        // furniture. That is not a defect: a search is always narrowed by its category before the terms are
        // applied, so what bounds an answer is the filter and never the words.
        alternatives.Should().Contain(product => product.Category != CatalogCategory.Chair);
    }

    [Fact] // the description is read by the expanded terms, and deliberately not by the search box
    public async Task A_word_only_a_description_carries_is_found_by_the_terms_and_not_by_the_typed_search()
    {
        var (catalogue, nameSearch) = Build();

        // "Lumbar" is a characteristic no product is named for: it is in three chairs' descriptions and in no name
        // at all. That makes it the case the description is indexed for, and the case the search box must not see.
        (await nameSearch.FindBestMatchesForTypedSearchWordsAsync(catalogue.All, "lumbar"))
            .Should().BeEmpty(
                "the search box answers from the name, so indexing the description cannot have widened what it finds");

        (await nameSearch.FindBestMatchesForExpandedSearchTermsAsync(catalogue.All, ["lumbar"]))
            .Should().Contain(product => product.Category == CatalogCategory.Chair,
                "a term an expansion produces is a characteristic, and a catalogue states those in prose");
    }

    [Fact]
    public async Task Terms_are_answered_in_the_order_they_were_given()
    {
        var (catalogue, nameSearch) = Build();

        var byName = await nameSearch.FindBestMatchesForExpandedSearchTermsAsync(catalogue.All, ["Kahuna"]);
        var bySecondTermAlone = await nameSearch.FindBestMatchesForExpandedSearchTermsAsync(catalogue.All, ["desk"]);

        var both = await nameSearch.FindBestMatchesForExpandedSearchTermsAsync(catalogue.All, ["Kahuna", "desk"]);

        // The caller's order is the ranking between terms: everything the first term found, then whatever the
        // second found that is new. That is what puts a primary term's matches above a synonym's.
        both.Take(byName.Count).Should().Equal(byName);
        both.Should().OnlyContain(product => byName.Contains(product) || bySecondTermAlone.Contains(product));
        both.Should().OnlyHaveUniqueItems("a product two terms both found is answered once");
    }

    [Fact]
    public async Task Terms_that_match_nothing_return_nothing()
    {
        var (catalogue, nameSearch) = Build();

        (await nameSearch.FindBestMatchesForExpandedSearchTermsAsync(
            catalogue.All, ["submarine periscope", "zeppelin"]))
            .Should().BeEmpty();
    }

    [Fact] // CAT-14 for the expanded rule
    public async Task Nothing_expanded_returns_everything_in_catalogue_order()
    {
        var (catalogue, nameSearch) = Build();

        (await nameSearch.FindBestMatchesForExpandedSearchTermsAsync(catalogue.All, []))
            .Should().Equal(catalogue.All);
    }

    [Fact] // CAT-13 — the caller's set is the boundary, whichever rule ranked it
    public async Task An_expanded_search_never_reaches_past_the_caller_s_set()
    {
        var (catalogue, nameSearch) = Build();

        var lampsOnly = catalogue.BySubCategory(CatalogSubCategory.Lamp);

        (await nameSearch.FindBestMatchesForExpandedSearchTermsAsync(lampsOnly, ["Kahuna"]))
            .Should().BeEmpty("the Kahuna chairs are not among the products the caller was given");
    }

    [Fact]
    public async Task The_same_terms_twice_give_the_same_answer()
    {
        var (catalogue, nameSearch) = Build();

        string[] expandedSearchTerms = ["standing desk", "workstation"];

        (await nameSearch.FindBestMatchesForExpandedSearchTermsAsync(catalogue.All, expandedSearchTerms))
            .Should().Equal(await nameSearch.FindBestMatchesForExpandedSearchTermsAsync(
                catalogue.All, expandedSearchTerms));
    }
}
