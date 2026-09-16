namespace CoreRentalNet.Host.Composition;

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
