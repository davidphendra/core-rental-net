using CoreRentalNet.BuildingBlocks.Infrastructure.Sqlite;
using CoreRentalNet.Host.Helpers;
using CoreRentalNet.Host.Presentation;
using CoreRentalNet.Modules.Catalog.Infrastructure;
using CoreRentalNet.Modules.Catalog.Application.Queries.GetProductByFeature;
using CoreRentalNet.Modules.Catalog.Application.Queries.GetProductBySku;
using CoreRentalNet.Modules.Catalog.Application.Queries.SearchCatalog;
using CoreRentalNet.Modules.Catalog.Application.Queries.SearchSimilarityCatalog;
using CoreRentalNet.Modules.Catalog.Application.Contracts;

namespace CoreRentalNet.Host.Extentions;

/// <summary>
/// The catalogService: its own SQLite store, read-only to callers.
/// </summary>
internal static class CatalogRegistrationExtentions
{
    public static void AddCatalog(this WebApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        AddCatalogueDatabase(builder);

        // The store is the catalogue now, read through the bounded port. The web root lets each row's
        // image be resolved as it is read, against this deployment's files.
        builder.Services.AddScoped<IProductCatalogService>(services => new SqliteProductCatalogService(
            services.GetRequiredService<CatalogContext>(),
            services.GetRequiredService<IWebHostEnvironment>().WebRootPath));

        // Registered by the operation each one performs, not by its own type: the UI reaches the
        // application through the interface, so how the page is produced stays the application's
        // business and the service's name never reaches a component.
        builder.Services.AddScoped<IGetFeaturedProductsHandler, GetFeaturedProductsHandler>();
        builder.Services.AddScoped<ISearchCatalogHandler, SearchCatalogHandler>();
        builder.Services.AddScoped<ISearchSimilarityCatalogHandler, SearchSimilarityCatalogHandler>();
        builder.Services.AddScoped<IGetProductBySkuHandler, GetProductBySkuHandler>();

        // Each component gets its own browsing state, which is why this is transient: a shared browser
        // would make the panel's category and the store's the same category.
        builder.Services.AddTransient(services => new CatalogBrowser(
            (tab, query) => CatalogTabs.Load(tab, services.GetRequiredService<ISearchCatalogHandler>(), query)));
    }

    /// <summary>
    /// The catalogue's own file. A second database rather than a slice of <c>corerental.db</c>, so the
    /// catalogue can be regenerated without touching the drafts and orders beside it.
    /// </summary>
    private static void AddCatalogueDatabase(WebApplicationBuilder builder)
    {
        var configured = builder.Configuration["Catalog:DatabasePath"] ?? "App_Data/catalog.db";

        var settings = new SqliteDatabaseSettings(
            Path.IsPathRooted(configured)
                ? configured
                : Path.Combine(builder.Environment.ContentRootPath, configured));

        // Opening once creates the directory and stamps the journal mode into the file.
        using (SqliteDatabase.Open(settings))
        {
        }

        builder.Services.AddDbContext<CatalogContext>((provider, options) =>
        {
            CatalogPersistence.Configure(options, settings);
            options.AddInterceptors(provider.GetRequiredService<SqlitePragmaInterceptor>());
        });
    }
}
