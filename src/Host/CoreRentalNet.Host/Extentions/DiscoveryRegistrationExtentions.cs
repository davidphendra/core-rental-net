using CoreRentalNet.BuildingBlocks.Application.Embeddings;
using CoreRentalNet.BuildingBlocks.Infrastructure.Embeddings;
using CoreRentalNet.BuildingBlocks.Infrastructure.Sqlite;
using CoreRentalNet.Host.Configs;
using CoreRentalNet.Modules.Catalog.Application.Contracts;
using CoreRentalNet.Modules.Discovery.Application.Indexing;
using CoreRentalNet.Modules.Discovery.Infrastructure;
using CoreRentalNet.Modules.Discovery.Infrastructure.Indexing;
using CoreRentalNet.Modules.Discovery.Infrastructure.NameSearch;
using CoreRentalNet.Modules.Discovery.Infrastructure.Vectors;
using Microsoft.EntityFrameworkCore;

namespace CoreRentalNet.Host.Extentions;

/// <summary>
/// The catalogue vectors: the store that holds them, the check that they may be searched, and the
/// search the catalogue answers with.
/// </summary>
/// <remarks>
/// <para>
/// <b>The key is read here and the client is built once.</b> The vectors are built by the ingestion tool
/// against the Azure OpenAI deployment, and this application embeds its queries through the same
/// deployment with the same key. The adapter that talks to it is shared rather than written twice, so a
/// query vector and a stored vector are produced by one implementation of one protocol.
/// </para>
/// <para>
/// <b>The vectors are this module's own store now.</b> The vector table and the recipe beside it live in
/// the Discovery database and are carried by its migrations; the application creates them at start-up
/// rather than being handed a file the ingestion tool built. What the tool writes after that is the same
/// tables, in the same file.
/// </para>
/// <para>
/// <b>An unconfigured deployment gets <see cref="EmbeddingNotConfigured"/> rather than nothing.</b> A
/// missing registration would surface as a container failure when the first search was attempted; this
/// surfaces as a search that says it is unavailable, which is what an unconfigured capability should look
/// like.
/// </para>
/// </remarks>
internal static class DiscoveryRegistrationExtentions
{
    public static void AddDiscovery(this WebApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        var settings = VectorEmbeddingSettings.From(builder.Configuration);

        builder.Services.AddSingleton(settings);

        builder.Services.AddSingleton<IEmbeddingService>(
            settings.IsConfigured
                ? OpenAiCompatibleEmbeddingService.Build(
                    new Uri(settings.Endpoint),
                    settings.ApiKey,
                    settings.Model,
                    settings.Width)
                : new EmbeddingNotConfigured());

        var store = AddEmbeddingStore(builder);

        builder.Services.AddSingleton<ICatalogIndexFreshnessService>(
            new CatalogIndexFreshnessService(store.DatabasePath));

        AddSimilaritySearch(builder, store.DatabasePath);

        // The name search: an index built once from the catalogService snapshot and ranked by BM25. A
        // singleton because it holds the index, and the catalogService it is built from is a singleton too.
        builder.Services.AddSingleton<IProductNameSearchService, LiftiProductNameSearchService>();
    }

    /// <summary>
    /// The vector store: this module's own file, its context, and the vec0 extension on its connections.
    /// </summary>
    /// <remarks>
    /// <b>The vec0 interceptor is registered for this context alone.</b> Only the vector store has a
    /// virtual table, and loading a native extension on the other modules' ordinary connections would let
    /// a missing vec0 binary break code that has nothing to do with vectors. The pragma interceptor is the
    /// shared one, so the store is opened under the same rules as every other file.
    /// </remarks>
    private static SqliteDatabaseSettings AddEmbeddingStore(WebApplicationBuilder builder)
    {
        var configured = builder.Configuration["Discovery:DatabasePath"] ?? "App_Data/discovery.db";

        var settings = new SqliteDatabaseSettings(
            Path.IsPathRooted(configured)
                ? configured
                : Path.Combine(builder.Environment.ContentRootPath, configured));

        builder.Services.AddSingleton<SqliteVectorInterceptor>();

        builder.Services.AddDbContext<EmbeddingContext>((provider, options) =>
        {
            EmbeddingPersistence.Configure(options, settings);
            options.AddInterceptors(provider.GetRequiredService<SqlitePragmaInterceptor>());
            options.AddInterceptors(provider.GetRequiredService<SqliteVectorInterceptor>());
        });

        return settings;
    }

    /// <summary>
    /// The search the catalogue answers with: the vectors the store holds, searched by a vector, refused
    /// while the index cannot be trusted.
    /// </summary>
    /// <remarks>
    /// <b>The guard is here rather than in the endpoint.</b> A stale index would rank products against vectors
    /// from another model or another catalogue and return a plausible answer; the decorator turns that into the
    /// same 503 an embedding outage produces, so the action has no failure branch and both failures look the
    /// same to a caller. The availability it reads is written by the start-up check, which runs after this.
    /// </remarks>
    private static void AddSimilaritySearch(WebApplicationBuilder builder, string databasePath)
    {
        builder.Services.AddSingleton<CatalogIndexAvailability>();

        builder.Services.AddSingleton<IProductSimilarityService>(provider => new GuardedProductSimilarityService(
            new SqliteProductSimilarityService(databasePath),
            provider.GetRequiredService<CatalogIndexAvailability>()));
    }
}
