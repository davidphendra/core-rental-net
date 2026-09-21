using AwesomeAssertions;
using CoreRentalNet.BuildingBlocks.Domain;
using CoreRentalNet.Modules.Catalog.Application.Contracts;
using CoreRentalNet.Modules.Discovery.Application;
using CoreRentalNet.Modules.Discovery.Application.Indexing;
using CoreRentalNet.Modules.Discovery.Application.Ingestion;
using CoreRentalNet.Modules.Discovery.Infrastructure;
using CoreRentalNet.Modules.Discovery.Infrastructure.Indexing;
using CoreRentalNet.Modules.Discovery.Infrastructure.Ingestion;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CoreRentalNet.IntegrationTests;

/// <summary>
/// Whether a stored index may be searched by this deployment.
/// </summary>
/// <remarks>
/// Each of the four recorded values is exercised separately, because each of them fails the same silent way:
/// a search over vectors built from another catalogue, another model, another width or another text still
/// returns fourteen products and simply ranks them wrongly. Nothing downstream notices, which is why this
/// check exists and why every one of the four has its own test rather than one test for "stale".
/// </remarks>
public sealed class CatalogIndexFreshnessTests
{
    private const int Width = 512;

    private const string Hash = "0f1e2d3c4b5a6978";

    private const string Model = "text-embedding-3-large";

    private static ProductView Product()
        => new(
            Sku: "DSKB08XN4JDR",
            Name: "HON Mod Desk Shell, Mahogany",
            Category: CatalogCategory.Desk,
            SubCategory: null,
            MonthlyPrice: new Money(266000m, Currencies.Idr),
            Description: "A wide desk shell for a working space.",
            Metadata: new CatalogMetadata(["desks"], new Dictionary<string, string> { ["brand"] = "HON" }, [], []),
            ImagePath: "images/desk.jpg",
            ImageAvailable: true,
            IsFeatured: false);

    private static async Task<DiscoveryContext> IndexedAsync(SqliteTestDatabase database)
    {
        var context = await database.CreateMigratedDiscoveryContextAsync();

        await new CatalogIngestion(new StandInEmbeddingClient(Width), context).IngestAsync(
            new CatalogIngestionRequest([Product()], Hash, Model, Width),
            CancellationToken.None);

        return context;
    }

    private static Task<CatalogIndexVerdict> CheckAsync(DiscoveryContext context, string hash = Hash, string model = Model, int width = Width)
        => new CatalogIndexFreshness(context).CheckAsync(hash, model, width, CancellationToken.None);

    [Fact] // SCR-02
    public async Task An_index_that_was_never_built_is_not_usable_and_names_the_tool()
    {
        await using var database = new SqliteTestDatabase();
        await using var context = await database.CreateMigratedDiscoveryContextAsync();

        var verdict = await CheckAsync(context);

        verdict.IsCurrent.Should().BeFalse();
        // "Never built" and "stale" hide the same feature and want different fixes, so the line says which.
        verdict.Reason.Should().Contain("CatalogIngestion");
    }

    [Fact] // SCR-02
    public async Task An_index_built_from_this_catalogue_and_this_deployment_is_usable()
    {
        await using var database = new SqliteTestDatabase();
        await using var context = await IndexedAsync(database);

        var verdict = await CheckAsync(context);

        verdict.IsCurrent.Should().BeTrue();
        verdict.Reason.Should().BeEmpty();
    }

    [Fact] // SCR-02
    public async Task A_catalogue_that_has_changed_since_the_index_was_built_is_stale()
    {
        await using var database = new SqliteTestDatabase();
        await using var context = await IndexedAsync(database);

        var verdict = await CheckAsync(context, hash: "ffffffffffffffff");

        verdict.IsCurrent.Should().BeFalse();
        verdict.Reason.Should().Contain("catalogue has changed").And.Contain(Hash).And.Contain("ffffffffffffffff");
    }

    [Fact] // SCR-02
    public async Task A_model_that_is_not_the_one_recorded_is_stale()
    {
        await using var database = new SqliteTestDatabase();
        await using var context = await IndexedAsync(database);

        var verdict = await CheckAsync(context, model: "text-embedding-3-small");

        // Vectors from two models are not comparable, so this is the case where a search would silently mean
        // nothing rather than fail.
        verdict.IsCurrent.Should().BeFalse();
        verdict.Reason.Should().Contain(Model).And.Contain("text-embedding-3-small");
    }

    [Fact] // SCR-02
    public async Task A_width_that_is_not_the_one_recorded_is_stale()
    {
        await using var database = new SqliteTestDatabase();
        await using var context = await IndexedAsync(database);

        var verdict = await CheckAsync(context, width: 1024);

        verdict.IsCurrent.Should().BeFalse();
        verdict.Reason.Should().Contain("512").And.Contain("1024");
    }

    [Fact] // SCR-02
    public async Task A_composition_that_this_build_no_longer_renders_is_stale()
    {
        await using var database = new SqliteTestDatabase();
        await using var context = await IndexedAsync(database);

        // Written directly, because the ingestion always records the composition the code renders - which is
        // exactly why this case cannot arise from the tool and can arise from a developer changing the fields.
        var recipe = await context.Indexes.SingleAsync();
        recipe.Composition = "name+description/0";
        await context.SaveChangesAsync();

        var verdict = await CheckAsync(context);

        verdict.IsCurrent.Should().BeFalse();
        verdict.Reason.Should().Contain("name+description/0").And.Contain(EmbeddedText.Composition);
    }
}
