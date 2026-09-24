using CoreRentalNet.BuildingBlocks.Application.Embeddings;
using CoreRentalNet.BuildingBlocks.Infrastructure.Embeddings;
using CoreRentalNet.Host.Configs;
using CoreRentalNet.Modules.Catalog.Application.Contracts;
using CoreRentalNet.Modules.Discovery.Application.Indexing;
using CoreRentalNet.Modules.Discovery.Infrastructure.Indexing;
using CoreRentalNet.Modules.Discovery.Infrastructure.NameSearch;
using CoreRentalNet.Modules.Discovery.Infrastructure.Vectors;

namespace CoreRentalNet.Host.Extentions;

/// <summary>
/// The catalogue vectors: the check that they may be searched, and the search the catalogue answers with.
/// </summary>
/// <remarks>
/// <para>
/// <b>Nothing here resolves a credential, because nothing here calls a cloud service.</b> The vectors are built
/// by the ingestion tool against a local OpenAI-compatible server, and this application embeds its queries
/// through the same one. The adapter that talks to it is shared rather than written twice, so a query vector
/// and a stored vector are produced by one implementation of one protocol.
/// </para>
/// <para>
/// <b>The vectors are read from the tool's own file, not from this application's database.</b> The tool owns
/// that file and its schema; this deployment only reads it, through the search the catalogue declares and this
/// composition root wires. The module keeps no table of its own.
/// </para>
/// <para>
/// <b>An unconfigured deployment gets <see cref="EmbeddingNotConfigured"/> rather than nothing.</b> A missing
/// registration would surface as a container failure when the first search was attempted; this surfaces as a
/// search that says it is unavailable, which is what an unconfigured capability should look like.
/// </para>
/// </remarks>
internal static class DiscoveryRegistrationExtentions
{
    public static void AddDiscovery(this WebApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        var settings = DiscoverySettings.From(builder.Configuration);

        builder.Services.AddSingleton(settings);

        builder.Services.AddSingleton<IEmbeddingService>(
            settings.IsConfigured
                ? OpenAiCompatibleEmbeddingService.Build(settings.Server, settings.Model)
                : new EmbeddingNotConfigured());

        var databasePath = ResolveDatabasePath(builder, settings.EmbeddingDatabase);

        builder.Services.AddSingleton<ICatalogIndexFreshnessService>(new CatalogIndexFreshnessService(databasePath));

        AddSimilaritySearch(builder, databasePath);

        // The name search: an index built once from the catalogService snapshot and ranked by BM25. A
        // singleton because it holds the index, and the catalogService it is built from is a singleton too.
        builder.Services.AddSingleton<IProductNameSearchService, LiftiProductNameSearchService>();
    }

    /// <summary>
    /// The search the catalogue answers with: the vectors the tool's file holds, searched by a vector, refused
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

    /// <summary>
    /// The tool's file, resolved against the content root so a relative path means one thing.
    /// </summary>
    /// <remarks>
    /// An unconfigured deployment gets the conventional location rather than a blank one, so the search's
    /// refusal names a real path and the tool that fills it.
    /// </remarks>
    private static string ResolveDatabasePath(WebApplicationBuilder builder, string configured)
    {
        var fallback = Path.Combine(builder.Environment.ContentRootPath, "App_Data", "product_embedding.db");

        if (string.IsNullOrWhiteSpace(configured))
        {
            return fallback;
        }

        return Path.IsPathRooted(configured)
            ? configured
            : Path.Combine(builder.Environment.ContentRootPath, configured);
    }
}
