using CoreRentalNet.Host.Agents;
using CoreRentalNet.Modules.Discovery.Application.Indexing;
using CoreRentalNet.Modules.Discovery.Infrastructure;

namespace CoreRentalNet.Host.Composition;

/// <summary>
/// Decides, once, whether the catalogue index this deployment would search is the one it has.
/// </summary>
/// <remarks>
/// <para>
/// <b>It hides the feature and does not stop the application.</b> The storefront, the catalogue and the orders
/// are not hostage to an optional feature's derived data, so an unusable index logs an error naming the tool to
/// run and the AI section is absent — exactly what <c>NoAgentConfigured</c> already does for a missing endpoint.
/// </para>
/// <para>
/// <b>Any failure hides, and that direction is deliberate.</b> At start-up the safe answer to "can I use this
/// index?" is no: hiding costs a feature, and throwing costs the whole application. The reason is logged either
/// way, so a genuine defect is visible in the log rather than silent — but it is not allowed to take the shop
/// down.
/// </para>
/// <para>
/// It runs after the migrations, because in development those are what create the index's tables, and it reads
/// the hash of the catalogue file as the application resolved it — the same file the ingestion tool hashed.
/// </para>
/// </remarks>
internal static class DiscoveryStartup
{
    private const string Category = "CoreRentalNet.Host.Composition.DiscoveryStartup";

    public static async Task GateSuggestionOnIndexAsync(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        var settings = app.Services.GetRequiredService<DiscoverySettings>();

        if (!settings.IsConfigured)
        {
            // Nothing to check: the feature is already hidden for want of configuration, and the embedding
            // client registered is the one that refuses.
            return;
        }

        var availability = app.Services.GetRequiredService<SuggestionAvailability>();
        var logger = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger(Category);

        string? reason;

        try
        {
            var verdict = await CheckAsync(app, settings);

            reason = verdict.IsCurrent ? null : verdict.Reason;
        }
        catch (Exception exception)
        {
            reason = $"the catalogue index could not be checked: {exception.Message}";
        }

        if (reason is not null)
        {
            Hide(availability, logger, reason);
        }
    }

    /// <summary>Whether the stored index matches what this deployment would build.</summary>
    private static Task<CatalogIndexVerdict> CheckAsync(WebApplication app, DiscoverySettings settings)
        => app.Services.GetRequiredService<ICatalogIndexFreshness>().CheckAsync(
            CatalogHash.OfFile(CatalogPaths.Resolve(app.Configuration)),
            settings.ModelId,
            settings.Width,
            CancellationToken.None);

    /// <summary>Hides the feature and says why, in the one place either happens.</summary>
    private static void Hide(SuggestionAvailability availability, ILogger logger, string reason)
    {
        availability.Hide(reason);

        logger.LogError(
            "The AI workspace builder is hidden because its catalogue index cannot be used. {Reason}",
            reason);
    }
}
