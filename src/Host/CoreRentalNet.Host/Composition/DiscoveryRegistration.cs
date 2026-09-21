using Azure.Identity;
using CoreRentalNet.BuildingBlocks.Infrastructure.Sqlite;
using CoreRentalNet.Host.AiBuilder;
using CoreRentalNet.Modules.Discovery.Application.Embeddings;
using CoreRentalNet.Modules.Discovery.Application.Selection;
using CoreRentalNet.Modules.Discovery.Application.Shortlist;
using CoreRentalNet.Modules.Discovery.Infrastructure;
using CoreRentalNet.Modules.Discovery.Infrastructure.Embeddings;
using CoreRentalNet.Modules.Discovery.Infrastructure.Selection;
using CoreRentalNet.Modules.Discovery.Infrastructure.Shortlist;
using CoreRentalNet.Modules.Workspace.Application.Commands.ReplaceComposition;
using Microsoft.EntityFrameworkCore;

namespace CoreRentalNet.Host.Composition;

/// <summary>
/// The catalogue index, and the shortlist a run is built from.
/// </summary>
/// <remarks>
/// <para>
/// <b>The credential is resolved here and nowhere else in this application.</b> The module's adapter takes a
/// credential rather than finding one, so the Host owns the identity it calls with — the same rule ADR 0002 sets
/// for the agent, applied to the second thing this application spends money on. <c>DefaultAzureCredential</c>
/// resolves to the developer's sign-in locally and to a managed identity when deployed.
/// </para>
/// <para>
/// <b>An unconfigured deployment gets <see cref="EmbeddingNotConfigured"/> rather than nothing.</b> A missing
/// registration would surface as a container failure when the first run was attempted; this surfaces as a run
/// that says it is unavailable, which is what an unconfigured capability should look like.
/// </para>
/// </remarks>
internal static class DiscoveryRegistration
{
    public static void AddDiscovery(this WebApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        var settings = DiscoverySettings.From(builder.Configuration);

        builder.Services.AddSingleton(settings);

        builder.Services.AddSingleton<IEmbeddingClient>(
            settings.IsConfigured
                ? FoundryEmbeddingClient.Build(
                    settings.ProjectEndpoint,
                    settings.Deployment,
                    settings.Width,
                    new DefaultAzureCredential())
                : new EmbeddingNotConfigured());

        // Built from the same settings the client above was built from, so the record cannot name a deployment
        // the run did not use.
        builder.Services.AddSingleton(new RetrievalFacts(settings.ModelId, settings.Width));

        // The knob the evaluation tier moves, and the reason the collapse-to-one-option rate is a measurable
        // number: a shortlist of two per bucket cannot also guarantee a cheap and a dear option everywhere.
        builder.Services.AddSingleton(new ShortlistSettings(settings.PerBucket, settings.BoostWeight));

        // Scoped, not singleton, and the lifetime is a consequence rather than a preference: the shortlist reads
        // the index through a DbContext, and a DbContext is scoped. Resolving it is per RUN anyway - one
        // shortlist per run - so nothing is rebuilt for a second caller. A singleton here fails the container's
        // own validation at start-up, which is how this was found.
        builder.Services.AddScoped<ICatalogShortlist, CatalogShortlist>();

        AddSelectionSignal(builder, settings);

        AddApplyCrediting(builder);
        AddContext(builder);
    }

    /// <summary>The module's tables, in the one file, configured exactly as every other module configures its own.</summary>
    private static void AddContext(WebApplicationBuilder builder)
        => builder.Services.AddDbContext<DiscoveryContext>((provider, options) =>
        {
            DiscoveryPersistence.Configure(options, provider.GetRequiredService<SqliteDatabaseSettings>());
            options.AddInterceptors(provider.GetRequiredService<SqlitePragmaInterceptor>());
        });

    /// <summary>
    /// Wraps the apply command so the composition a customer saves is credited to the products in it.
    /// </summary>
    /// <remarks>
    /// Registered LAST, and registered here rather than in <c>WorkspaceRegistration</c>, because it is a
    /// Discovery concern wearing a Workspace interface: the workspace module must not know that a selection
    /// signal exists. The concrete handler is registered so the decorator can be given the real one — resolving
    /// the interface inside itself would be a cycle.
    /// </remarks>
    private static void AddApplyCrediting(WebApplicationBuilder builder)
    {
        builder.Services.AddScoped<ReplaceCompositionHandler>();
        builder.Services.AddScoped<IReplaceCompositionHandler>(provider => new CreditingReplaceComposition(
            provider.GetRequiredService<ReplaceCompositionHandler>(),
            provider.GetRequiredService<ICreditSelection>(),
            provider.GetRequiredService<ILoggerFactory>()));
    }

    /// <summary>The two writes that make retrieval remember what customers take.</summary>
    /// <remarks>
    /// Scoped for the same reason the shortlist is — they read and write through the scoped context — and one
    /// clock for both, so the two counters of a row can never be decayed by different notions of now.
    /// </remarks>
    private static void AddSelectionSignal(WebApplicationBuilder builder, DiscoverySettings settings)
    {
        builder.Services.AddSingleton(new SelectionSettings(TimeSpan.FromDays(settings.HalfLifeDays)));
        builder.Services.AddSingleton(TimeProvider.System);
        builder.Services.AddScoped<IOfferSelection, SelectionOffers>();
        builder.Services.AddScoped<ICreditSelection, SelectionCredit>();
    }
}
