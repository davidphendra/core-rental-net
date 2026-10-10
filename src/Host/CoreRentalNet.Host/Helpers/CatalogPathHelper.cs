namespace CoreRentalNet.Host.Helpers;

/// <summary>Where the catalogService file is. A path decision, not application wiring.</summary>
internal static class CatalogPathHelper
{
    private const string DefaultCatalogFilePath = "App_Data/products.json";

    public static string Resolve(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var configured = configuration["Catalog:FilePath"];

        return string.IsNullOrWhiteSpace(configured)
            ? Path.Combine(AppContext.BaseDirectory, DefaultCatalogFilePath)
            : configured;
    }
}
