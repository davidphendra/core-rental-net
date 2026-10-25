namespace CoreRentalNet.Host.Helpers;

/// <summary>Where the catalogue database is. A path decision, not application wiring.</summary>
internal static class CatalogPathHelper
{
    private const string DefaultCatalogFilePath = "App_Data/catalog.db";

    public static string Resolve(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var configured = configuration["Catalog:DatabasePath"];

        return string.IsNullOrWhiteSpace(configured)
            ? Path.Combine(AppContext.BaseDirectory, DefaultCatalogFilePath)
            : configured;
    }
}
