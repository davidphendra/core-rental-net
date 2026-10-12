using CoreRentalNet.CatalogPopulate.Sources;

namespace CoreRentalNet.CatalogPopulate.Selection;

/// <summary>The seven categories the catalogue is drawn from, and how a source row is matched to one.</summary>
internal static class ProductCategories
{
    public static readonly SourceCategory Desk =
        new("Desk", "DSK", "desk", null, "OfficeProducts", "OfficeProducts", 50);

    public static readonly SourceCategory Chair =
        new("Chair", "CHA", "chair", null, "OfficeProducts", "OfficeProducts", 60);

    public static readonly SourceCategory Monitor =
        new("Monitor", "MON", "accessory", "monitor", "Electronics", "Electronics", 58);

    public static readonly SourceCategory Lamp =
        new("Lamp", "LMP", "accessory", "lamp", "ToolsAndHomeImprovement", "ToolsAndHomeImprovement", 58);

    public static readonly SourceCategory Plant =
        new("Plant", "PLT", "accessory", "plant", "HomeAndKitchen", "HomeAndKitchen", 58);

    public static readonly SourceCategory Coffee =
        new("Coffee", "CFE", "accessory", "coffee", "HomeAndKitchen", "HomeAndKitchen", 58);

    public static readonly SourceCategory Beanbag =
        new("Beanbag", "BBG", "accessory", "beanbag", "HomeAndKitchen", "HomeAndKitchen", 58);

    /// <summary>Every category, in the order the catalogue lists them.</summary>
    public static readonly IReadOnlyList<SourceCategory> All =
        [Desk, Chair, Monitor, Lamp, Plant, Coffee, Beanbag];

    private static readonly IReadOnlyDictionary<string, SourceCategory> s_byPrefix =
        All.ToDictionary(category => category.Prefix, StringComparer.Ordinal);

    /// <summary>The category a source row belongs to, or null when it is not one of the seven.</summary>
    /// <remarks>
    /// The more specific phrases are tested first, so a "desk chair" is a chair and a "bean bag chair" is a
    /// bean bag. The path is preferred to the title, because the path is the marketplace's own classification.
    /// </remarks>
    public static SourceCategory? Match(AmazonMetaRecord meta)
    {
        var path = string.Join(" > ", meta.Categories ?? []).ToLowerInvariant();
        var title = (meta.Title ?? string.Empty).ToLowerInvariant();

        return Contains(path, title, "bean bag", "beanbag", "bean bag")
            ?? Contains(path, title, "coffee", "coffee")
            ?? Contains(path, title, "artificial plant", "artificial plants", "plant")
            ?? Contains(path, title, "lamps", "lamp")
            ?? Contains(path, title, "monitors", "monitor")
            ?? Contains(path, title, "chairs", "chair")
            ?? Contains(path, title, "desks", "workstations", "desk");
    }

    /// <summary>The category a stored prefix belongs to.</summary>
    public static SourceCategory ByPrefix(string prefix)
        => s_byPrefix.TryGetValue(prefix, out var category)
            ? category
            : throw new ArgumentException($"Unknown catalogue prefix '{prefix}'.");

    private static SourceCategory? Contains(string path, string title, string pathPhrase, params string[] titleWords)
    {
        if (path.Contains(pathPhrase, StringComparison.Ordinal))
        {
            return For(pathPhrase);
        }

        foreach (var word in titleWords)
        {
            if (title.Contains(word, StringComparison.Ordinal))
            {
                return For(pathPhrase);
            }
        }

        return null;
    }

    private static SourceCategory? For(string phrase) => phrase switch
    {
        "bean bag" or "beanbag" => Beanbag,
        "coffee" => Coffee,
        "artificial plant" or "artificial plants" or "plant" => Plant,
        "lamps" or "lamp" => Lamp,
        "monitors" or "monitor" => Monitor,
        "chairs" or "chair" => Chair,
        "desks" or "workstations" or "desk" => Desk,
        _ => null,
    };
}
