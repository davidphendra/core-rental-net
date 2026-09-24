using AwesomeAssertions;
using CoreRentalNet.BuildingBlocks.Infrastructure.Hashing;
using CoreRentalNet.BuildingBlocks.Infrastructure.Vectors;
using CoreRentalNet.Modules.Catalog.Infrastructure;
using Xunit;

namespace CoreRentalNet.IntegrationTests;

/// <summary>
/// Holds the committed golden fixture against the catalogue and the contract it claims to have been built from.
/// </summary>
/// <remarks>
/// <para>
/// <b>The vectors are a snapshot, so something has to say when the snapshot stops describing reality.</b> A
/// fixture whose vectors were built from a catalogue that has since changed would keep passing while asserting
/// against products nobody has any more - the silent staleness the ingestion tool's freshness check exists to
/// prevent, one layer down. These guards are that check for the fixture.
/// </para>
/// <para>
/// <b>A failure here is fixed by re-running <c>regenerate-catalog-embedding-fixture.py</c></b>, not by relaxing
/// the guard. That is the same obligation the recipe table imposes on the tool's own vectors.
/// </para>
/// </remarks>
public sealed class CatalogEmbeddingFixtureGuardTests
{
    private const int HexCharacters = 64;

    private static readonly CatalogEmbeddingFixture Fixture = CatalogEmbeddingFixture.Load();

    private static string ProductsJson => RepoRoot.Combine("src", "shared", "data", "products.json");

    [Fact]
    public void The_fixture_was_built_from_the_catalogue_that_is_here_now()
    {
        Fixture.Recipe.CatalogueHash.Should().Be(
            CatalogHash.OfFile(ProductsJson),
            "the vectors describe a catalogue that has changed; regenerate the fixture");
    }

    [Fact]
    public void The_fixture_records_the_text_contract_and_width_the_tool_writes()
    {
        Fixture.Recipe.ModelId.Should().NotBeNullOrWhiteSpace();
        Fixture.Recipe.Composition.Should().Be(
            ProductVectorContract.Composition,
            "a renderer change bumps the composition, and the fixture has to bump with it");
        Fixture.Recipe.Width.Should().Be(384, "the model the fixture was embedded with is 384 wide");
    }

    [Fact]
    public void Every_fixture_product_still_matches_the_catalogue()
    {
        var catalogue = new ProductCatalogService(ProductsJson, null);

        foreach (var product in Fixture.Products)
        {
            var real = catalogue.Find(product.Sku);

            real.Should().NotBeNull($"'{product.Sku}' is in the fixture and must still exist");

            real!.Name.Should().Be(product.Name);
            ((int)real.MonthlyPrice.Amount).Should().Be(product.PricePerMonth);
            real.Category.ToString().Should().BeEquivalentTo(product.Category);
            real.SubCategory?.ToString().Should().BeEquivalentTo(product.SubCategory);
            product.TextHash.Should().HaveLength(HexCharacters, "a rendered text's hash is a SHA-256, lower-case hex");
        }
    }

    [Fact]
    public void Every_vector_is_the_width_the_fixture_records()
    {
        var expected = Fixture.Recipe.Width * sizeof(float);

        AllVectors().Should().OnlyContain(vector => vector.Length == expected);
    }

    [Fact]
    public void Every_expected_product_and_every_phrase_is_in_the_fixture()
    {
        var skus = Fixture.Products.Select(product => product.Sku).ToArray();

        Fixture.Queries.Should().OnlyContain(query => skus.Contains(query.ExpectedSku));
        Fixture.Queries.Should().OnlyContain(query => Fixture.Products.Any(product =>
            string.Equals(product.Category, query.Category, StringComparison.OrdinalIgnoreCase)
            && (query.SubCategory == null
                || string.Equals(product.SubCategory, query.SubCategory, StringComparison.OrdinalIgnoreCase))),
            "every query's narrowing must have at least one product behind it");
        Fixture.Products.Select(product => product.Sku).Should().OnlyHaveUniqueItems(
            "one passage per product in the fixture, so a duplicate would silently change the ranking");
    }

    private static IEnumerable<byte[]> AllVectors()
        => Fixture.Products.Select(product => product.Vector)
            .Concat(Fixture.Queries.Select(query => query.Vector))
            .Concat(Fixture.Characterisation.Select(phrase => phrase.Vector));
}
