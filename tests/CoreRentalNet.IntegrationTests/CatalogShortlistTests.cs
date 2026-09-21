using AwesomeAssertions;
using CoreRentalNet.Modules.Catalog.Infrastructure;
using CoreRentalNet.Modules.Discovery.Application.Ingestion;
using CoreRentalNet.Modules.Discovery.Application.Shortlist;
using CoreRentalNet.Modules.Discovery.Infrastructure;
using CoreRentalNet.Modules.Discovery.Infrastructure.Ingestion;
using CoreRentalNet.Modules.Discovery.Infrastructure.Shortlist;
using Xunit;

namespace CoreRentalNet.IntegrationTests;

/// <summary>
/// The shortlist end to end: a real catalogue, a real index file, and a stand-in deployment.
/// </summary>
/// <remarks>
/// The ranking's rules are unit-tested where they live. What this adds is the wiring — that the vectors come
/// back out of the file, that each product's bucket is read from the catalogue rather than stored, and that the
/// promise of two per bucket holds against the real 205 rather than against a fixture built to make it hold.
/// </remarks>
public sealed class CatalogShortlistTests
{
    private const int Width = 512;

    private static string ProductsJson => RepoRoot.Combine("src", "shared", "data", "products.json");

    /// <summary>Puts an index of the real catalogue into the given context, and returns that catalogue.</summary>
    private static async Task<ProductCatalog> IndexedAsync(DiscoveryContext context)
    {
        var catalogue = new ProductCatalog(ProductsJson, webRootPath: null);

        await new CatalogIngestion(new StandInEmbeddingClient(Width), context).IngestAsync(
            new CatalogIngestionRequest(catalogue.All, "0f1e2d3c4b5a6978", "text-embedding-3-large", Width),
            CancellationToken.None);

        return catalogue;
    }

    [Fact] // SCR-08
    public async Task The_shortlist_takes_two_from_every_bucket_of_the_real_catalogue()
    {
        await using var database = new SqliteTestDatabase();
        await using var context = await database.CreateMigratedDiscoveryContextAsync();
        var catalogue = await IndexedAsync(context);

        var shortlist = await new CatalogShortlist(
                new StandInEmbeddingClient(Width),
                catalogue,
                context,
                ShortlistSettings.Default)
            .ForAsync("a quiet corner where I can work with two screens", CancellationToken.None);

        shortlist.Should().HaveCount(14, "seven buckets of this catalogue each have products, and two come from each");
        shortlist.GroupBy(item => item.Bucket).Should().HaveCount(7);
        shortlist.GroupBy(item => item.Bucket).Should().OnlyContain(bucket => bucket.Count() == 2);
        shortlist.Select(item => item.Sku).Should().OnlyHaveUniqueItems();
        shortlist.Select(item => item.Sku).Should().OnlyContain(sku => catalogue.Find(sku) != null);
    }

    [Fact] // SCR-11
    public async Task The_sentence_reaches_the_deployment_verbatim_and_alone()
    {
        // The only honest way to prove a string was not rewritten is to look at what the deployment was handed.
        // The sentence is deliberately given irregular spacing: a trimmed or re-composed version would come back
        // different, and a prefixing convention borrowed from another model family would show up as part of it.
        await using var database = new SqliteTestDatabase();
        await using var context = await database.CreateMigratedDiscoveryContextAsync();
        var catalogue = await IndexedAsync(context);

        const string Sentence = "  a quiet corner, with two screens  ";
        var embeddings = new StandInEmbeddingClient(Width);

        await new CatalogShortlist(embeddings, catalogue, context, ShortlistSettings.Default)
            .ForAsync(Sentence, CancellationToken.None);

        // One call, carrying one text, equal to the sentence: no prefix, no rewriting, and no catalogue text
        // mixed in with it.
        embeddings.Calls.Should().ContainSingle();
        embeddings.Calls[0].Should().Equal(Sentence);
    }
}
