using AwesomeAssertions;
using CoreRentalNet.BuildingBlocks.Domain;
using CoreRentalNet.Modules.Catalog.Application.Contracts;
using CoreRentalNet.Modules.Discovery.Application;
using CoreRentalNet.Modules.Discovery.Application.Embeddings;
using CoreRentalNet.Modules.Discovery.Application.Ingestion;
using CoreRentalNet.Modules.Discovery.Infrastructure;
using CoreRentalNet.Modules.Discovery.Infrastructure.Ingestion;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CoreRentalNet.IntegrationTests;

/// <summary>
/// Building the index, against a real SQLite file and a stand-in embedding deployment.
/// </summary>
/// <remarks>
/// The stand-in means nothing about quality — it is the ingestion's logic under test, not the retrieval's.
/// What is proved here is the part that has to be exact: every product becomes one row, the recipe describes
/// those rows, running it twice replaces rather than appends, and an unusable answer is refused before
/// anything is written.
/// </remarks>
public sealed class CatalogIngestionTests
{
    private const int Width = 512;

    private const string Hash = "0f1e2d3c4b5a6978";

    private const string Model = "text-embedding-3-large";

    private static IReadOnlyList<ProductView> Catalogue() =>
    [
        Product("DSKB08XN4JDR", "HON Mod Desk Shell, Mahogany"),
        Product("CHA449AGLBB0", "HON Chairman Chair, Black"),
        Product("MONJVAP81NPQ", "Dell 24 Monitor, Black"),
    ];

    private static ProductView Product(string sku, string name)
        => new(
            Sku: sku,
            Name: name,
            Category: CatalogCategory.Desk,
            SubCategory: null,
            MonthlyPrice: new Money(266000m, Currencies.Idr),
            Description: $"A {name} for a working space.",
            Metadata: new CatalogMetadata(
                ["desks", "workstations"],
                new Dictionary<string, string> { ["brand"] = "HON", ["rating"] = "5.0" },
                ["general office work"],
                []),
            ImagePath: "images/desk.jpg",
            ImageAvailable: true,
            IsFeatured: false);

    private static CatalogIngestionRequest Request(IReadOnlyList<ProductView> catalogue, int width = Width)
        => new(catalogue, Hash, Model, width);

    [Fact]
    public async Task The_ingestion_writes_one_vector_per_product_and_the_recipe_that_produced_them()
    {
        await using var database = new SqliteTestDatabase();
        await using var context = await database.CreateMigratedDiscoveryContextAsync();

        var written = await new CatalogIngestion(new StandInEmbeddingClient(Width), context)
            .IngestAsync(Request(Catalogue()), CancellationToken.None);

        written.Should().Be(3);
        (await context.Vectors.CountAsync()).Should().Be(3);

        var recipe = await context.Indexes.SingleAsync();
        recipe.Name.Should().Be(CatalogIngestion.IndexName);
        recipe.ModelId.Should().Be(Model);
        recipe.Width.Should().Be(Width);
        recipe.Composition.Should().Be(EmbeddedText.Composition);
        recipe.CatalogueHash.Should().Be(Hash);
    }

    [Fact]
    public async Task The_texts_embedded_are_the_ones_the_recorded_composition_produces()
    {
        await using var database = new SqliteTestDatabase();
        await using var context = await database.CreateMigratedDiscoveryContextAsync();
        var embeddings = new StandInEmbeddingClient(Width);
        var catalogue = Catalogue();

        await new CatalogIngestion(embeddings, context).IngestAsync(Request(catalogue), CancellationToken.None);

        // One call, carrying every product: the deployment is batched and so is this.
        embeddings.Calls.Should().HaveCount(1);
        embeddings.Calls[0].Should().Equal(catalogue.Select(EmbeddedText.Of).ToArray());
    }

    [Fact]
    public async Task Running_the_ingestion_again_replaces_rather_than_appends()
    {
        await using var database = new SqliteTestDatabase();
        await using var context = await database.CreateMigratedDiscoveryContextAsync();
        var ingestion = new CatalogIngestion(new StandInEmbeddingClient(Width), context);

        await ingestion.IngestAsync(Request(Catalogue()), CancellationToken.None);

        var smaller = Catalogue().Take(2).ToArray();
        await ingestion.IngestAsync(Request(smaller), CancellationToken.None);

        // Two, not five: a second run is a replacement, which is what makes the tool safe to re-run.
        (await context.Vectors.CountAsync()).Should().Be(2);
        (await context.Indexes.CountAsync()).Should().Be(1);

        var skus = await context.Vectors.Select(vector => vector.Sku).ToListAsync();
        skus.Should().BeEquivalentTo(smaller.Select(product => product.Sku));
    }

    [Fact]
    public async Task A_short_answer_from_the_deployment_is_refused_and_the_existing_index_survives()
    {
        await using var database = new SqliteTestDatabase();
        await using var context = await database.CreateMigratedDiscoveryContextAsync();

        await new CatalogIngestion(new StandInEmbeddingClient(Width), context)
            .IngestAsync(Request(Catalogue()), CancellationToken.None);

        // One vector for three products. Accepting it would replace a working index with a partial one, and
        // the query path would then answer from two thirds of a catalogue without anything failing.
        var truncated = new StandInEmbeddingClient(Width, answerWith: 1);
        Func<Task> act = () => new CatalogIngestion(truncated, context).IngestAsync(Request(Catalogue()), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*partial index*");

        (await context.Vectors.CountAsync()).Should().Be(3);
        (await context.Indexes.SingleAsync()).CatalogueHash.Should().Be(Hash);
    }

    [Fact]
    public async Task A_vector_of_the_wrong_width_is_refused_and_the_existing_index_survives()
    {
        await using var database = new SqliteTestDatabase();
        await using var context = await database.CreateMigratedDiscoveryContextAsync();

        await new CatalogIngestion(new StandInEmbeddingClient(Width), context)
            .IngestAsync(Request(Catalogue()), CancellationToken.None);

        // The deployment answers 256 wide where 512 was declared. Inferring the width from the answer would
        // record a number the query path cannot then rely on.
        var narrow = new StandInEmbeddingClient(Width / 2);
        Func<Task> act = () => new CatalogIngestion(narrow, context).IngestAsync(Request(Catalogue()), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*was declared*");

        (await context.Vectors.CountAsync()).Should().Be(3);
    }

    [Fact]
    public async Task An_empty_catalogue_is_refused()
    {
        await using var database = new SqliteTestDatabase();
        await using var context = await database.CreateMigratedDiscoveryContextAsync();

        Func<Task> act = () => new CatalogIngestion(new StandInEmbeddingClient(Width), context)
            .IngestAsync(Request([]), CancellationToken.None);

        // An index of nothing answers every query with no products and looks like a working deployment.
        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*nothing to index*");
    }
}
