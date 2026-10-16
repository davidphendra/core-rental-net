namespace CoreRentalNet.CatalogPopulate;

/// <summary>Everything one run was configured with, already resolved and validated.</summary>
internal sealed record CatalogPopulateSettings(
    Mode Mode,
    string CompactInputPath,
    string CompactOutputPath,
    string MigrationsDirectory,
    string VendoredImagesPath,
    string MetaOfficeProductsPath,
    string MetaElectronicsPath,
    string MetaToolsAndHomeImprovementPath,
    string MetaHomeAndKitchenPath,
    string ReviewOfficeProductsPath,
    string ReviewElectronicsPath,
    string ReviewToolsAndHomeImprovementPath,
    string ReviewHomeAndKitchenPath,
    string LlmEndpoint,
    string LlmModel,
    int ReviewSampleSize,
    int TargetTotal)
{
    /// <summary>The compact input written by --extract and read when populating.</summary>
    public const string DefaultCompactPath = "App_Data/compact-catalogue.jsonl";

    /// <summary>The folder holding the SeedProducts migration shell, relative to the tool's project directory.</summary>
    public const string DefaultMigrationsDirectory =
        "../../Modules/Catalog/CoreRentalNet.Modules.Catalog.Infrastructure/Migrations";

    /// <summary>The application's vendored image directory, relative to the tool's project directory.</summary>
    public const string DefaultVendoredImagesPath = "../../Host/CoreRentalNet.Host/wwwroot/images/vendored";

    /// <summary>The local OpenAI-compatible server the ingestion tool already points at.</summary>
    public const string DefaultLlmEndpoint = "http://localhost:8080/v1";

    /// <summary>The model that writes the generated metadata.</summary>
    public const string DefaultLlmModel = "Qwen3-Coder-30B-A3B-Instruct";

    /// <summary>How many reviews one product's prompt may carry.</summary>
    public const int DefaultReviewSampleSize = 20;

    /// <summary>How many products the compact input holds: the pinned set plus spare candidates.</summary>
    public const int DefaultTargetTotal = 400;
}
