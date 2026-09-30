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

    /// <summary>The price words, measured rather than asserted.</summary>
    public static TheoryData<string> PriceWords
    {
        get
        {
            var data = new TheoryData<string>();

            foreach (var phrase in Fixture.Characterisation)
            {
                data.Add(phrase.Phrase);
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

    [Theory]
    [MemberData(nameof(Kinds))]
    public async Task An_unfiltered_paraphrase_ranks_its_product_in_the_top_three(string kind)
    {
        var query = Fixture.Query(kind);

        var products = await SearchAsync(query.Phrase);

        products.Should().NotBeEmpty();
        products.Take(3).Select(product => product.Sku).Should().Contain(
            query.ExpectedSku,
            "across the whole catalogue the right product is still near the top - this is the ranking rule S2");
    }

    [Fact]
    public async Task The_name_search_finds_nothing_where_the_similarity_search_finds_the_product()
    {
        // The proof that this is semantic and not lexical: the same sentence, through the name search, matches
        // no product at all - and through the similarity search it finds the ergonomic chair first.
        var query = Fixture.Query("chair");
        var catalogue = new ProductCatalogService(ProductsJson, null);

        (await new LiftiProductNameSearchService(catalogue).FindBestMatchesForTypedSearchWordsAsync(catalogue.All, query.Phrase))
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

        var order = (await SearchAsync(query.Phrase)).Select(product => product.Sku).ToList();
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

    [Theory]
    [MemberData(nameof(PriceWords))]
    public async Task A_price_word_does_not_reach_the_dearest_product(string phrase)
    {
        // RECORDED GAP, not a quality claim. The embedding model has no notion of price: nothing in a product's
        // text says "luxury", so a price word is near nothing in particular. This pins that fact so a change of
        // model fails here rather than passing quietly, and it is why the suite asserts paraphrases and not
        // price synonyms.
        var dearest = Fixture.Products.MaxBy(product => product.PricePerMonth)!;

        var products = await SearchAsync(phrase);

        products.Should().NotBeEmpty("the search still answers; it simply has no price to go on");
        products.Take(3).Select(product => product.Sku).Should().NotContain(
            dearest.Sku,
            "the dearest product is not what a price word retrieves");
    }

    private static Task<IReadOnlyList<ProductView>> SearchAsync(CatalogEmbeddingFixtureQuery query)
        => SearchAsync(query.Phrase, CategoryOf(query.Category), SubCategoryOf(query.SubCategory));

    private static async Task<IReadOnlyList<ProductView>> SearchAsync(
        string phrase,
        CatalogCategory? category = null,
        CatalogSubCategory? subCategory = null)
    {
        using var file = new IngestedVectorFile();

        foreach (var product in Fixture.Products)
        {
            file.WithVector(product.Sku, VectorBlob.ToFloats(product.Vector));
        }

        var handler = new SearchSimilarityCatalogHandler(
            new ProductCatalogService(ProductsJson, null),
            new FixtureEmbeddingService(Fixture),
            new SqliteProductSimilarityService(file.Path));

        return await handler.HandleAsync(
            new SearchSimilarityCatalogQuery(category, subCategory, phrase),
            CancellationToken.None);
    }

    private static bool Matches(ProductView product, CatalogEmbeddingFixtureQuery query)
        => (query.Category is null || string.Equals(product.Category.ToString(), query.Category, StringComparison.OrdinalIgnoreCase))
           && (query.SubCategory is null || string.Equals(product.SubCategory?.ToString(), query.SubCategory, StringComparison.OrdinalIgnoreCase));

    private static CatalogCategory? CategoryOf(string? value)
        => value is null ? null : Enum.Parse<CatalogCategory>(value, ignoreCase: true);

    private static CatalogSubCategory? SubCategoryOf(string? value)
        => value is null ? null : Enum.Parse<CatalogSubCategory>(value, ignoreCase: true);
}
