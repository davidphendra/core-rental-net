using AwesomeAssertions;
using CoreRentalNet.Modules.Catalog.Application.Contracts;
using CoreRentalNet.Modules.Catalog.Infrastructure;
using CoreRentalNet.Modules.Discovery.Infrastructure.NameSearch;
using Xunit;

namespace CoreRentalNet.IntegrationTests;

/// <summary>
/// What the expansion is for, measured over the real catalogue: a described need expressed as several terms
/// finds products, and the same need expressed as one typed phrase finds none.
/// </summary>
/// <remarks>
/// <para>
/// This is the acceptance evidence for the change rather than a property of the index. Both halves matter: the
/// first says the pipeline can now find what the customer described, and the second says the defect it replaced
/// was real — a stated need reaching a name search as a sentence, where every word must appear and a product's
/// name rarely contains all of them.
/// </para>
/// <para>
/// The terms below are what the rephraser is asked to produce for one sentence of each kind — one to three words
/// a catalogue title could contain, not a description. Measured when the change was made over the 205-product
/// catalogue: the expansion found 10 to 28 products per component, and the phrase found none of seven.
/// </para>
/// </remarks>
public sealed class ExpandedSearchRecallTests
{
    private static string ProductsJson => RepoRoot.Combine("src", "shared", "data", "products.json");

    /// <summary>One component: the catalogue arguments, the terms, and the need as one typed phrase.</summary>
    private static readonly ComponentSearch[] DescribedWorkspace =
    [
        new(CatalogCategory.Desk, null,
            ["standing desk", "computer desk", "writing desk"],
            "a wide stable standing surface for programming"),
        new(CatalogCategory.Chair, null,
            ["ergonomic chair", "office chair", "task chair"],
            "ergonomic seating for long programming sessions"),
        new(CatalogCategory.Accessory, CatalogSubCategory.Monitor,
            ["computer monitor", "widescreen monitor", "lcd monitor"],
            "a large computer display for multitasking"),
        new(CatalogCategory.Accessory, CatalogSubCategory.Lamp,
            ["desk lamp", "table lamp", "floor lamp"],
            "adjustable lighting for late evening work"),
        new(CatalogCategory.Accessory, CatalogSubCategory.Plant,
            ["artificial plant", "indoor plant", "potted plant"],
            "a low maintenance plant for a calm corner"),
        new(CatalogCategory.Accessory, CatalogSubCategory.Coffee,
            ["coffee machine", "espresso machine", "coffee maker"],
            "a quiet espresso machine for the office"),
        new(CatalogCategory.Accessory, CatalogSubCategory.Beanbag,
            ["bean bag", "beanbag chair", "lounge chair"],
            "a soft lounge seat for reading"),
    ];

    [Fact] // the change's whole purpose, one component at a time
    public async Task Every_described_component_finds_products_by_its_terms()
    {
        var (catalogue, nameSearch) = Build();

        foreach (var component in DescribedWorkspace)
        {
            var productsMatchingTheFilters = catalogue.Search(component.Category, component.SubCategory);

            var found = await nameSearch.FindBestMatchesForExpandedSearchTermsAsync(
                productsMatchingTheFilters, component.SearchTerms);

            found.Should().NotBeEmpty(
                $"{component.SubCategory?.ToString() ?? component.Category.ToString()} must be findable by the "
                + $"terms an expansion would carry ({string.Join(", ", component.SearchTerms)})");

            found.Should().OnlyContain(product =>
                product.Category == component.Category
                && product.SubCategory == component.SubCategory,
                "the terms cannot reach past the filters the search was narrowed by");
        }
    }

    [Fact] // the same seven needs as one typed phrase each: nothing, every time
    public async Task The_same_needs_as_one_typed_phrase_find_nothing()
    {
        var (catalogue, nameSearch) = Build();

        foreach (var component in DescribedWorkspace)
        {
            var productsMatchingTheFilters = catalogue.Search(component.Category, component.SubCategory);

            var found = await nameSearch.FindBestMatchesForTypedSearchWordsAsync(
                productsMatchingTheFilters, component.NeedAsOnePhrase);

            found.Should().BeEmpty(
                $"'{component.NeedAsOnePhrase}' is a description, and a name search requires every word of it");
        }
    }

    [Fact] // the terms are not a widening trick: asking for something the catalogue lacks still finds nothing
    public async Task Terms_that_describe_nothing_in_the_catalogue_still_find_nothing()
    {
        var (catalogue, nameSearch) = Build();

        var found = await nameSearch.FindBestMatchesForExpandedSearchTermsAsync(
            catalogue.Search(CatalogCategory.Desk, null),
            ["submarine periscope", "zeppelin mooring"]);

        found.Should().BeEmpty();
    }

    private static (ProductCatalogService Catalogue, LiftiProductNameSearchService NameSearch) Build()
    {
        var catalogue = new ProductCatalogService(ProductsJson, null);

        return (catalogue, new LiftiProductNameSearchService(catalogue));
    }

    /// <summary>One component of a described workspace, with both readings of the same need.</summary>
    private sealed record ComponentSearch(
        CatalogCategory Category,
        CatalogSubCategory? SubCategory,
        IReadOnlyList<string> SearchTerms,
        string NeedAsOnePhrase);
}
