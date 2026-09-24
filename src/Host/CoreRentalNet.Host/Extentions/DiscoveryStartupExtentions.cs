using CoreRentalNet.BuildingBlocks.Infrastructure.Hashing;
using CoreRentalNet.BuildingBlocks.Infrastructure.Vectors;
using CoreRentalNet.Host.Configs;
using CoreRentalNet.Host.Helpers;
using CoreRentalNet.Modules.Discovery.Application.Indexing;

namespace CoreRentalNet.Host.Extentions;

/// <summary>
/// Decides, once, whether the catalogue index this deployment would search is the one it has.
/// </summary>
/// <remarks>
/// <para>
/// <b>It refuses the feature and does not stop the application.</b> The storefront, the catalogue and the orders
/// are not hostage to an optional feature's derived data, so an unusable index logs an error naming the tool to
/// run and the similarity search answers that it is unavailable.
/// </para>
/// <para>
/// <b>Any failure refuses, and that direction is deliberate.</b> At start-up the safe answer to "can I use this
/// index?" is no: refusing costs a feature, and throwing costs the whole application. The reason is logged
/// either way, so a genuine defect is visible in the log rather than silent — but it is not allowed to take the
/// shop down.
/// </para>
/// <para>
/// It reads the hash of the catalogue file as the application resolved it — the same file the ingestion tool
/// hashed.
/// </para>
/// </remarks>
internal static class DiscoveryStartupExtentions
{
    private const string Category = "CoreRentalNet.Host.Composition.DiscoveryStartupExtentions";

    public static void GateSimilarityOnIndex(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        var settings = app.Services.GetRequiredService<DiscoverySettings>();

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
            var verdict = Check(app, settings);

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

    /// <summary>Whether the stored vectors match what this deployment would build.</summary>
    private static CatalogIndexVerdict Check(WebApplication app, DiscoverySettings settings)
        => app.Services.GetRequiredService<ICatalogIndexFreshnessService>().Check(
            CatalogHash.OfFile(CatalogPathHelper.Resolve(app.Configuration)),
            settings.Model,
            settings.Width,
            ProductVectorContract.Composition);
}
