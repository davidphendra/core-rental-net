namespace CoreRentalNet.CatalogPopulate.Transform;

/// <summary>The one rule that turns a source id into the catalogue's SKU.</summary>
/// <remarks>
/// Verified against the committed catalogue: every SKU is the three-letter category prefix followed by the
/// first nine characters of the source <c>parent_asin</c>.
/// </remarks>
internal static class ProductIdentifier
{
    /// <summary>How many characters of the source id the SKU keeps.</summary>
    public const int SourceIdLength = 9;

    /// <summary>The SKU for one source product.</summary>
    public static string Sku(string prefix, string parentAsin)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(prefix);
        ArgumentException.ThrowIfNullOrWhiteSpace(parentAsin);

        var kept = parentAsin.Length <= SourceIdLength ? parentAsin : parentAsin[..SourceIdLength];

        return prefix + kept;
    }
}
