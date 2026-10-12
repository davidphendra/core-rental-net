using System.Text.Json;
using CoreRentalNet.CatalogPopulate.Compact;
using CoreRentalNet.CatalogPopulate.Selection;
using CoreRentalNet.CatalogPopulate.Sources;
using CoreRentalNet.CatalogPopulate.Transform;

namespace CoreRentalNet.CatalogPopulate.Extracting;

/// <summary>Scans the source files once and writes the compact input the populator reads.</summary>
internal sealed class Extractor(CatalogPopulateSettings settings)
{
    /// <summary>Runs the extraction, returning a process exit code.</summary>
    public int Run()
    {
        var byCategory = ProductCategories.All.ToDictionary(category => category.Key, _ => new List<AmazonMetaRecord>(), StringComparer.Ordinal);

        foreach (var (fileKey, path) in MetaFiles())
        {
            foreach (var row in JsonLineReader.Read<AmazonMetaRecord>(path))
            {
                if (string.IsNullOrWhiteSpace(row.ParentAsin) || string.IsNullOrWhiteSpace(row.Title))
                {
                    continue;
                }

                var category = ProductCategories.Match(row);
                if (category is not null && category.MetaFileKey == fileKey)
                {
                    byCategory[category.Key].Add(row);
                }
            }
        }

        var selected = Select(byCategory);
        var reviews = ReadReviews(selected.Keys);

        var products = ProductCategories.All
            .SelectMany(category => selected
                .Where(pair => pair.Value.Category.MetaFileKey == category.MetaFileKey && pair.Value.Category.Key == category.Key)
                .OrderBy(pair => ProductIdentifier.Sku(category.Prefix, pair.Key), StringComparer.Ordinal)
                .Select(pair => ToCompact(pair.Value.Category, pair.Value.Meta, reviews)))
            .ToList();

        CompactProductStore.Write(settings.CompactOutputPath, products);
        Console.WriteLine($"[extract] wrote {products.Count} products to {settings.CompactOutputPath}");

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

    private Dictionary<string, List<CompactReview>> ReadReviews(IEnumerable<string> asins)
    {
        var wanted = new HashSet<string>(asins, StringComparer.Ordinal);
        var reviews = wanted.ToDictionary(asin => asin, _ => new List<CompactReview>(), StringComparer.Ordinal);

        foreach (var (_, path) in ReviewFiles())
        {
            foreach (var row in JsonLineReader.Read<AmazonReviewRecord>(path))
            {
                if (row.ParentAsin is not null && wanted.Contains(row.ParentAsin))
                {
                    reviews[row.ParentAsin].Add(new CompactReview(row.Rating, row.Title, row.Text, row.HelpfulVote, row.Timestamp));
                }
            }
        }

        return reviews;
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
