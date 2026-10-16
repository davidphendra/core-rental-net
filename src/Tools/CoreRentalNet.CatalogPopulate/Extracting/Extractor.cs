using System.Text.Json;
using CoreRentalNet.CatalogPopulate.Compact;
using CoreRentalNet.CatalogPopulate.Reporting;
using CoreRentalNet.CatalogPopulate.Selection;
using CoreRentalNet.CatalogPopulate.Sources;
using CoreRentalNet.CatalogPopulate.Transform;

namespace CoreRentalNet.CatalogPopulate.Extracting;

/// <summary>Scans the source files once and writes the compact input the populator reads.</summary>
internal sealed class Extractor(CatalogPopulateSettings settings)
{
    /// <summary>Runs the extraction, returning a process exit code.</summary>
    public int Run(IProgressReporter reporter)
    {
        var byCategory = new MetaScanner(reporter).Scan(MetaFiles());

        reporter.Report(new ProgressUpdate("Selecting products", 0, 1, string.Empty));
        var selected = Select(byCategory);
        reporter.Report(new ProgressUpdate(
            "Selecting products", 1, 1, $"{selected.Count} selected across {ProductCategories.All.Count} categories"));

        var reviews = new ReviewReader(reporter).Read(ReviewFiles(), selected.Keys);

        var products = ProductCategories.All
            .SelectMany(category => selected
                .Where(pair => pair.Value.Category.MetaFileKey == category.MetaFileKey && pair.Value.Category.Key == category.Key)
                .OrderBy(pair => ProductIdentifier.Sku(category.Prefix, pair.Key), StringComparer.Ordinal)
                .Select(pair => ToCompact(pair.Value.Category, pair.Value.Meta, reviews)))
            .ToList();

        reporter.Report(new ProgressUpdate("Writing compact input", 0, 1, settings.CompactOutputPath));
        CompactProductStore.Write(settings.CompactOutputPath, products);
        reporter.Report(new ProgressUpdate(
            "Writing compact input", 1, 1, $"{products.Count} products → {settings.CompactOutputPath}"));

        reporter.Complete($"[extract] wrote {products.Count} products to {settings.CompactOutputPath}");

        return 0;
    }

    private List<(string FileKey, string Path)> MetaFiles() =>
    [
        ("OfficeProducts", settings.MetaOfficeProductsPath),
        ("Electronics", settings.MetaElectronicsPath),
        ("ToolsAndHomeImprovement", settings.MetaToolsAndHomeImprovementPath),
        ("HomeAndKitchen", settings.MetaHomeAndKitchenPath),
    ];

    private List<(string FileKey, string Path)> ReviewFiles() =>
    [
        ("OfficeProducts", settings.ReviewOfficeProductsPath),
        ("Electronics", settings.ReviewElectronicsPath),
        ("ToolsAndHomeImprovement", settings.ReviewToolsAndHomeImprovementPath),
        ("HomeAndKitchen", settings.ReviewHomeAndKitchenPath),
    ];

    private Dictionary<string, (SourceCategory Category, AmazonMetaRecord Meta)> Select(
        Dictionary<string, List<AmazonMetaRecord>> byCategory)
    {
        var selected = new Dictionary<string, (SourceCategory, AmazonMetaRecord)>(StringComparer.Ordinal);

        foreach (var category in ProductCategories.All)
        {
            var ordered = byCategory[category.Key]
                .OrderByDescending(row => row.RatingNumber ?? 0)
                .ThenBy(row => row.ParentAsin, StringComparer.Ordinal)
                .ToList();

            var pinnedAsins = ordered
                .Where(row => PinnedProducts.Contains(ProductIdentifier.Sku(category.Prefix, row.ParentAsin!)))
                .Select(row => row.ParentAsin!)
                .ToHashSet(StringComparer.Ordinal);

            var room = Math.Max(0, category.TargetTotal - pinnedAsins.Count);
            var extras = ordered.Where(row => !pinnedAsins.Contains(row.ParentAsin!)).Take(room);

            foreach (var row in ordered.Where(row => pinnedAsins.Contains(row.ParentAsin!)).Concat(extras))
            {
                selected[row.ParentAsin!] = (category, row);
            }
        }

        foreach (var surplus in selected.Keys.Skip(settings.TargetTotal).ToList())
        {
            selected.Remove(surplus);
        }

        return selected;
    }

    private static CompactProduct ToCompact(
        SourceCategory category,
        AmazonMetaRecord meta,
        Dictionary<string, List<CompactReview>> reviews)
    {
        var rows = reviews.TryGetValue(meta.ParentAsin!, out var found) ? found : [];

        var compact = new CompactMeta(
            meta.ParentAsin,
            meta.Title,
            meta.Description ?? [],
            meta.Categories ?? [],
            meta.Details ?? new Dictionary<string, JsonElement>(),
            meta.Price,
            meta.AverageRating,
            meta.RatingNumber,
            meta.Store,
            meta.Images?.FirstOrDefault()?.Large);

        return new CompactProduct(meta.ParentAsin!, category.Key, compact, rows, rows.Count);
    }
}
