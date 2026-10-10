using CoreRentalNet.BuildingBlocks.Infrastructure.Hashing;
using CoreRentalNet.BuildingBlocks.Infrastructure.Vectors;
using CoreRentalNet.Host.Configs;
using CoreRentalNet.Host.Helpers;
using CoreRentalNet.Modules.Discovery.Application.Indexing;

namespace CoreRentalNet.Host.Extentions;

internal static class DiscoveryStartupExtentions
{
    private const string Category = "CoreRentalNet.Host.Composition.DiscoveryStartupExtentions";

    public static void CheckSimilarityOnVectorEmbedding(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        var settings = app.Services.GetRequiredService<VectorEmbeddingSettings>();

        if (!settings.IsConfigured)
        {
            // Nothing to check: the feature is already unavailable for want of configuration, and the embedding
            // client registered is the one that refuses.
            return;
        }

        var index = app.Services.GetRequiredService<CatalogIndexAvailability>();
        var logger = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger(Category);

        string? reason;

        try
        {
            var catalogIndexFreshnessService = app.Services.GetRequiredService<ICatalogIndexFreshnessService>();
            var verdict = catalogIndexFreshnessService.Check(
                CatalogHash.OfFile(CatalogPathHelper.Resolve(app.Configuration)),
                settings.Model,
                settings.Width,
                ProductVectorContract.Composition);;

            reason = verdict.IsCurrent ? null : verdict.Reason;
        }
        catch (Exception exception)
        {
            reason = $"the catalogue index could not be checked: {exception.Message}";
        }

        if (reason is not null)
        {
            index.Refuse(reason);

            logger.LogError(
                "The catalogue similarity search is unavailable because its index cannot be used. {Reason}",
                reason);
        }
    }
}
