using System.Reflection;

namespace CoreRentalNet.CatalogPopulate.Selection;

/// <summary>The catalogue's existing SKUs, so a regeneration can never lose a product the application ships.</summary>
internal static class PinnedProducts
{
    private const string ResourceName = "CoreRentalNet.CatalogPopulate.Resources.pinned-skus.txt";

    private static readonly Lazy<HashSet<string>> s_skus = new(Load);

    /// <summary>True when the given SKU is one the catalogue already carries.</summary>
    public static bool Contains(string sku) => s_skus.Value.Contains(sku);

    private static HashSet<string> Load()
    {
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(ResourceName)
            ?? throw new InvalidOperationException($"Embedded resource '{ResourceName}' is missing.");
        using var reader = new StreamReader(stream);

        var skus = new HashSet<string>(StringComparer.Ordinal);
        for (var line = reader.ReadLine(); line is not null; line = reader.ReadLine())
        {
            if (!string.IsNullOrWhiteSpace(line))
            {
                skus.Add(line.Trim());
            }
        }

        return skus;
    }
}
