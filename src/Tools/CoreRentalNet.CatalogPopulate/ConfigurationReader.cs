using System.Globalization;
using Microsoft.Extensions.Configuration;

namespace CoreRentalNet.CatalogPopulate;

/// <summary>Turns the tool's configuration into <see cref="CatalogPopulateSettings"/>, or refuses the run.</summary>
/// <remarks>
/// An absent key takes its default; a value that is present but unusable stops the run. The two modes need
/// different things — extraction needs the four source files and no model, populating needs a model and no
/// source files — so only what the chosen mode uses is required. Paths are resolved against a base directory
/// the caller supplies, so the reader stays a pure function of its inputs.
/// </remarks>
internal static class ConfigurationReader
{
    public static CatalogPopulateSettings Read(IConfiguration configuration, string baseDirectory, Mode mode)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentException.ThrowIfNullOrWhiteSpace(baseDirectory);

        var extracting = mode == Mode.Extract;
        var (metaOffice, metaElectronics, metaTools, metaHome, reviewOffice, reviewElectronics, reviewTools, reviewHome) =
            Sources(configuration, baseDirectory, extracting);

        return new CatalogPopulateSettings(
            mode,
            Resolved(configuration, "Catalog:CompactInput", CatalogPopulateSettings.DefaultCompactPath, baseDirectory),
            Resolved(configuration, "Extract:Output", CatalogPopulateSettings.DefaultCompactPath, baseDirectory),
            Resolved(configuration, "Catalog:MigrationsDirectory", CatalogPopulateSettings.DefaultMigrationsDirectory, baseDirectory),
            Resolved(configuration, "Images:VendoredPath", CatalogPopulateSettings.DefaultVendoredImagesPath, baseDirectory),
            metaOffice,
            metaElectronics,
            metaTools,
            metaHome,
            reviewOffice,
            reviewElectronics,
            reviewTools,
            reviewHome,
            Text(configuration, "Llm:Endpoint", CatalogPopulateSettings.DefaultLlmEndpoint),
            Text(configuration, "Llm:Model", CatalogPopulateSettings.DefaultLlmModel),
            Positive(configuration, "Llm:ReviewSampleSize", CatalogPopulateSettings.DefaultReviewSampleSize),
            Positive(configuration, "Selection:TargetTotal", CatalogPopulateSettings.DefaultTargetTotal));
    }

    private static (string, string, string, string, string, string, string, string) Sources(
        IConfiguration configuration,
        string baseDirectory,
        bool extracting)
    {
        if (!extracting)
        {
            return (string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty);
        }

        return (
            Source(configuration, "Sources:Meta:OfficeProducts", baseDirectory),
            Source(configuration, "Sources:Meta:Electronics", baseDirectory),
            Source(configuration, "Sources:Meta:ToolsAndHomeImprovement", baseDirectory),
            Source(configuration, "Sources:Meta:HomeAndKitchen", baseDirectory),
            Source(configuration, "Sources:Reviews:OfficeProducts", baseDirectory),
            Source(configuration, "Sources:Reviews:Electronics", baseDirectory),
            Source(configuration, "Sources:Reviews:ToolsAndHomeImprovement", baseDirectory),
            Source(configuration, "Sources:Reviews:HomeAndKitchen", baseDirectory));
    }

    private static string Source(IConfiguration configuration, string key, string baseDirectory)
    {
        var stated = configuration[key];

        return string.IsNullOrWhiteSpace(stated)
            ? throw new ArgumentException($"{key} is not configured. Point it at the source file, or run with an action that does not need it.")
            : Path.IsPathRooted(stated) ? stated : Path.Combine(baseDirectory, stated);
    }

    private static string Text(IConfiguration configuration, string key, string fallback)
    {
        var stated = configuration[key];

        if (stated is null)
        {
            return fallback;
        }

        return string.IsNullOrWhiteSpace(stated)
            ? throw new ArgumentException($"{key} was set to a blank value. Remove it to take the default, or give it a value.")
            : stated.Trim();
    }

    private static int Positive(IConfiguration configuration, string key, int fallback)
    {
        var stated = configuration[key];

        if (stated is null)
        {
            return fallback;
        }

        return int.TryParse(stated, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value) && value > 0
            ? value
            : throw new ArgumentException($"{key} must be a positive whole number, but was '{value}'.");
    }

    private static string Resolved(IConfiguration configuration, string key, string fallback, string baseDirectory)
    {
        var stated = Text(configuration, key, fallback);

        return Path.IsPathRooted(stated) ? stated : Path.Combine(baseDirectory, stated);
    }
}
