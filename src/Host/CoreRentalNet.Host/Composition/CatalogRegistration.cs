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

        builder.Services.AddScoped<GetFeaturedProductsHandler>();
        builder.Services.AddScoped<GetCatalogPageHandler>();
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
