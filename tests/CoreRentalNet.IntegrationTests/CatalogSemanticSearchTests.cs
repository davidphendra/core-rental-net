using AwesomeAssertions;
using CoreRentalNet.BuildingBlocks.Infrastructure.Vectors;
using CoreRentalNet.Modules.Catalog.Application.Contracts;
using CoreRentalNet.Modules.Catalog.Application.Queries.SearchSimilarityCatalog;
using CoreRentalNet.Modules.Catalog.Infrastructure;
using CoreRentalNet.Modules.Discovery.Infrastructure.NameSearch;
using CoreRentalNet.Modules.Discovery.Infrastructure.Vectors;
using Xunit;

namespace CoreRentalNet.IntegrationTests;

/// <summary>
/// The semantic search at work: a sentence a customer might type finds the product it describes, in words the
/// product never uses.
/// </summary>
/// <remarks>
/// <para>
/// <b>Every phrase is a paraphrase with no keyword overlap with the product it must find.</b> That is what
/// makes this a test of the embedding rather than of a substring: <c>a big furry bean bag to sink into</c>
/// shares no word with <c>BizChair Oversized White Furry Refillable Bean Bag Chair</c>, and the name search
/// finds nothing for it while the similarity search finds it first.
/// </para>
/// <para>
/// <b>The vectors are the real model's, pinned in the fixture.</b> The ranking is sqlite-vec's, over real
/// cosine vectors, exercised through the real handler. What is stood in for is only the call to the embedding
/// server — <see cref="FixtureEmbeddingService"/> serves the recorded phrase vector. See
/// <see cref="CatalogEmbeddingFixture"/> for why, and <c>regenerate-catalog-embedding-fixture.py</c> for how
/// the pin is remade.
/// </para>
/// </remarks>
public sealed class CatalogSemanticSearchTests
{
    private static readonly CatalogEmbeddingFixture Fixture = CatalogEmbeddingFixture.Load();

    private static string ProductsJson => RepoRoot.Combine("src", "shared", "data", "products.json");

    /// <summary>One case per kind the fixture covers: three categories and five subcategories.</summary>
    public static TheoryData<string> Kinds
    {
        get
        {
            var data = new TheoryData<string>();

            foreach (var query in Fixture.Queries)
            {
                data.Add(query.Kind);
            }

            return data;
        }
    }


    [Theory]
    [MemberData(nameof(Kinds))]
    public async Task A_paraphrase_finds_its_product_within_its_kind(string kind)
    {
        var query = Fixture.Query(kind);

        var products = await SearchAsync(query);

        products.Should().NotBeEmpty("the paraphrase reached the catalogue and something was near it");
        products[0].Sku.Should().Be(query.ExpectedSku, "the product the sentence describes comes first");
        products.Should().OnlyContain(product => Matches(product, query), "the narrowing kept every other kind out");
    }

    [Fact]
    public async Task The_name_search_finds_nothing_where_the_similarity_search_finds_the_product()
    {
        // The proof that this is semantic and not lexical: the same sentence, through the name search, matches
        // no product at all - and through the similarity search it finds the ergonomic chair first.
        var query = Fixture.Query("chair");
        var catalogue = new JsonCatalogSource(ProductsJson, null);

        (await new LiftiProductNameSearchService().FindBestMatchesForTypedSearchWordsAsync(catalogue.All, query.Phrase))
            .Should().BeEmpty("no product's name contains 'my back hurts after sitting all day'");

        var products = await SearchAsync(query);

        products[0].Sku.Should().Be(query.ExpectedSku, "the meaning is what found it");
    }

    [Fact]
    public async Task A_monitor_phrase_returns_no_lamp()
    {
        // Cross-subcategory leakage: the kinds that share a category must not bleed into one another.
        var query = Fixture.Query("monitor");

        var products = await SearchAsync(query);

        products.Should().OnlyContain(product => product.SubCategory == CatalogSubCategory.Monitor);
        products.Should().NotContain(product => product.SubCategory == CatalogSubCategory.Lamp);
    }

    [Fact]
    public async Task A_monitor_phrase_ranks_the_monitor_above_both_lamps()
    {
        // The same leakage, unfiltered: the monitor must come before the lamps on its own, not only because a
        // filter removed them.
        var query = Fixture.Query("monitor");

        var order = (await SearchAcrossTheKindAsync(query)).Select(product => product.Sku).ToList();
        var monitor = order.IndexOf(query.ExpectedSku);

        monitor.Should().BeGreaterThanOrEqualTo(0);

        foreach (var lamp in Fixture.Products.Where(product => product.Kind == "lamp"))
        {
            var at = order.IndexOf(lamp.Sku);

            if (at >= 0)
            {
                monitor.Should().BeLessThan(at, $"the monitor must outrank {lamp.Sku}, a lamp");
            }
        }
    }

    private static Task<IReadOnlyList<ProductDto>> SearchAsync(CatalogEmbeddingFixtureQuery query)
        => SearchAsync(query.Phrase, CategoryOfExpected(query), SubCategoryOf(query.SubCategory));

    /// <summary>The same phrase, scoped to the whole kind so a subcategory does not hide its sibling kinds.</summary>
    private static Task<IReadOnlyList<ProductDto>> SearchAcrossTheKindAsync(CatalogEmbeddingFixtureQuery query)
        => SearchAsync(query.Phrase, CategoryOfExpected(query), null);

    /// <summary>The category a query narrows to, or the expected product's own when the query named none.</summary>
    private static CatalogCategory CategoryOfExpected(CatalogEmbeddingFixtureQuery query)
        => CategoryOf(query.Category) ?? CategoryForSku(query.ExpectedSku);

    private static CatalogCategory CategoryForSku(string sku)
        => Enum.Parse<CatalogCategory>(Fixture.Products.Single(product => product.Sku == sku).Category, ignoreCase: true);

    private static async Task<IReadOnlyList<ProductDto>> SearchAsync(
        string phrase,
        CatalogCategory category,
        CatalogSubCategory? subCategory = null)
    {
        using var file = new IngestedVectorFile();

        foreach (var product in Fixture.Products)
        {
            file.WithVector(product.Sku, VectorBlob.ToFloats(product.Vector));
        }

        var handler = new SearchSimilarityCatalogHandler(
            new JsonCatalogSource(ProductsJson, null),
            new FixtureEmbeddingService(Fixture),
            new SqliteProductSimilarityService(file.Path));

        return await handler.HandleAsync(
            new SearchSimilarityCatalogQuery(new CatalogScope(category, subCategory), phrase),
            CancellationToken.None);
    }

    private static bool Matches(ProductDto product, CatalogEmbeddingFixtureQuery query)
        => (query.Category is null || string.Equals(product.Category.ToString(), query.Category, StringComparison.OrdinalIgnoreCase))
           && (query.SubCategory is null || string.Equals(product.SubCategory?.ToString(), query.SubCategory, StringComparison.OrdinalIgnoreCase));

    private static CatalogCategory? CategoryOf(string? value)
        => value is null ? null : Enum.Parse<CatalogCategory>(value, ignoreCase: true);

    private static CatalogSubCategory? SubCategoryOf(string? value)
        => value is null ? null : Enum.Parse<CatalogSubCategory>(value, ignoreCase: true);
}
