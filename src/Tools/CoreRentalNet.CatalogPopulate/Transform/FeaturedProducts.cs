namespace CoreRentalNet.CatalogPopulate.Transform;

/// <summary>The products the catalogue marks as featured. Exactly two, by an earlier catalogue decision.</summary>
internal static class FeaturedProducts
{
    /// <summary>The badge a featured product carries.</summary>
    public const string Badge = "Popular";

    private static readonly HashSet<string> s_skus = new(StringComparer.Ordinal)
    {
        "DSKB08R35RW3",
        "LMPB08PV4PLK",
    };

    /// <summary>The badge for a SKU, or null when it is not featured.</summary>
    public static string? BadgeFor(string sku) => s_skus.Contains(sku) ? Badge : null;
}
