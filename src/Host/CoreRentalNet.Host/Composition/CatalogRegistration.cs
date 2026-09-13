using CoreRentalNet.Host.Presentation;
using CoreRentalNet.Modules.Catalog.Application.Contracts;
using CoreRentalNet.Modules.Catalog.Application.Queries;
using CoreRentalNet.Modules.Catalog.Domain;
using CoreRentalNet.Modules.Catalog.Infrastructure;
using CoreRentalNet.Modules.Catalog.Infrastructure.Loading;

namespace CoreRentalNet.Host.Composition;

/// <summary>
/// The catalog: read-only, loaded once from its file. It has no database at all (ADR-0005).
/// </summary>
internal static class CatalogRegistration
{
    public static void AddCatalog(this WebApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        var catalog = CatalogLoader.LoadFromFile(
            CatalogPaths.Resolve(builder.Configuration),
            builder.Environment.WebRootPath);

        builder.Services.AddSingleton<IProductCatalog>(catalog);
        builder.Services.AddSingleton<IDefineProductPrices>(new DefineProductPrices(catalog));

        // Registered by the operation each one performs, not by its own type: the UI reaches the
        // application through the interface, so how the page is produced stays the application's
        // business and the handler's name never reaches a component.
        builder.Services.AddScoped<IGetFeaturedProducts, GetFeaturedProductsHandler>();
        builder.Services.AddScoped<IGetCatalogPage, GetCatalogPageHandler>();

        // Each component gets its own browsing state, which is why this is transient: a shared browser
        // would make the panel's category and the store's the same category.
        builder.Services.AddTransient(services => new CatalogBrowser(
            (tab, query) => CatalogTabs.Load(tab, services.GetRequiredService<IGetCatalogPage>(), query)));
    }
}

/// <summary>Where the catalog file is. A path decision, not application wiring.</summary>
internal static class CatalogPaths
{
    private const string DevelopmentFallback = "Catalog/products.json";

    public static string Resolve(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var configured = configuration["Catalog:FilePath"];

        return string.IsNullOrWhiteSpace(configured)
            ? Path.Combine(AppContext.BaseDirectory, DevelopmentFallback)
            : configured;
    }
}
