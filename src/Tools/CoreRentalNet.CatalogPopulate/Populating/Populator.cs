using CoreRentalNet.CatalogPopulate.Catalog;
using CoreRentalNet.CatalogPopulate.Compact;
using CoreRentalNet.CatalogPopulate.Images;
using CoreRentalNet.CatalogPopulate.Metadata;
using CoreRentalNet.CatalogPopulate.Reporting;
using CoreRentalNet.CatalogPopulate.Selection;
using CoreRentalNet.CatalogPopulate.Transform;
using CoreRentalNet.CatalogPopulate.Writing;

namespace CoreRentalNet.CatalogPopulate.Populating;

/// <summary>Reads the compact input, asks the local model for each product's metadata, and writes the seed migration.</summary>
/// <remarks>
/// Every product is sent to the configured local model. There is no substitution here: the generator is the
/// OpenAI-compatible client, and a run without a reachable model fails rather than writing a catalogue whose
/// generated fields came from anywhere but the model.
/// </remarks>
internal sealed class Populator(CatalogPopulateSettings settings)
{
    /// <summary>Runs the populate, returning a process exit code.</summary>
    public async Task<int> RunAsync(IProgressReporter reporter)
    {
        reporter.Report(new ProgressUpdate("Loading compact input", 0, 1, settings.CompactInputPath));
        var compact = CompactProductStore.Read(settings.CompactInputPath).ToList();
        reporter.Report(new ProgressUpdate("Loading compact input", 1, 1, $"{compact.Count} products"));

        var transformer = new ProductTransformer();

        using var llm = Client();
        var generator = new OpenAiCompatibleMetadataGenerator(llm, settings.LlmModel);

        var rows = new List<(SourceCategory Category, ProductRecord Record)>();
        for (var index = 0; index < compact.Count; index++)
        {
            var product = compact[index];
            var category = ProductCategories.All.First(candidate => candidate.Key == product.CategoryKey);
            var sku = ProductIdentifier.Sku(category.Prefix, product.ParentAsin);
            reporter.Report(new ProgressUpdate(
                "Generating metadata", index, compact.Count, $"{sku} · {product.Meta.Title}"));

            var generated = await generator.GenerateAsync(
                new MetadataGenerationRequest(product, Sample(product)),
                CancellationToken.None);

            rows.Add((category, transformer.Transform(product, category, generated)));
        }

        reporter.Report(new ProgressUpdate(
            "Generating metadata", compact.Count, compact.Count, $"{compact.Count} products"));

        reporter.Report(new ProgressUpdate("Ordering catalogue", 0, 1, string.Empty));
        var ordered = rows
            .OrderBy(row => Index(row.Category))
            .ThenBy(row => row.Record.SkuNo, StringComparer.Ordinal)
            .Select(row => row.Record)
            .ToList();
        reporter.Report(new ProgressUpdate("Ordering catalogue", 1, 1, $"{ordered.Count} rows"));

        var migrationFile = SeedMigrationFile.In(settings.MigrationsDirectory).Path;
        reporter.Report(new ProgressUpdate("Writing migration", 0, 1, migrationFile));
        SeedMigrationWriter.Write(migrationFile, ordered);
        reporter.Report(new ProgressUpdate(
            "Writing migration", 1, 1, $"{ordered.Count} rows → {migrationFile}"));

        using var images = new HttpClient();
        var vendored = await new ImageVendor(images, settings.VendoredImagesPath).VendorAsync(
            ordered.ToDictionary(row => row.SkuNo!, row => row.Image, StringComparer.Ordinal),
            reporter,
            CancellationToken.None);

        reporter.Complete(
            $"[populate] wrote {ordered.Count} products to {migrationFile}; "
            + $"images {vendored.Downloaded} downloaded, {vendored.Kept} kept, {vendored.Removed} removed");

        return 0;
    }

    private IReadOnlyList<CompactReview> Sample(CompactProduct product)
        => product.Reviews
            .OrderByDescending(review => review.HelpfulVote ?? 0)
            .ThenByDescending(review => review.Timestamp ?? 0)
            .Take(settings.ReviewSampleSize)
            .ToList();

    private static int Index(SourceCategory category) => ProductCategories.All.ToList().IndexOf(category);

    private HttpClient Client()
        => new() { BaseAddress = new Uri(settings.LlmEndpoint.TrimEnd('/') + "/") };
}
