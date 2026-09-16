using CoreRentalNet.Host.Presentation;
using CoreRentalNet.Modules.Catalog.Infrastructure;
using CoreRentalNet.Modules.Catalog.Application.Queries.GetProductByFeature;
using CoreRentalNet.Modules.Catalog.Application.Queries.GetProductBySku;
using CoreRentalNet.Modules.Catalog.Application.Queries.SearchCatalog;
using CoreRentalNet.Modules.Catalog.Application.Contracts;

namespace CoreRentalNet.Host.Composition;

/// <summary>
/// The catalog: read-only, loaded once from its file. It has no database at all.
/// </summary>
internal static class CatalogRegistration
{
    public static void AddCatalog(this WebApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        // The catalogue is an immutable snapshot, so one instance serves every request. The web root
        // lets each product's image be resolved once, at load, rather than on every page.
        builder.Services.AddSingleton<IProductCatalog>(
            new ProductCatalog(CatalogPaths.Resolve(builder.Configuration), builder.Environment.WebRootPath));

        // Registered by the operation each one performs, not by its own type: the UI reaches the
        // application through the interface, so how the page is produced stays the application's
        // business and the service's name never reaches a component.
        builder.Services.AddScoped<IGetFeaturedProductsHandler, GetFeaturedProductsHandler>();
        builder.Services.AddScoped<ISearchCatalogHandler, SearchCatalogHandler>();
        builder.Services.AddScoped<IGetProductBySkuHandler, GetProductBySkuHandler>();

        // Each component gets its own browsing state, which is why this is transient: a shared browser
        // would make the panel's category and the store's the same category.
        builder.Services.AddTransient(services => new CatalogBrowser(
            (tab, query) => CatalogTabs.Load(tab, services.GetRequiredService<ISearchCatalogHandler>(), query)));
    }
}
