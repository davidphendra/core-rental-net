using CoreRentalNet.Modules.Catalog.Domain;

namespace CoreRentalNet.Modules.Catalog.Infrastructure.Loading;

/// <summary>The path to use for a product's image, and whether that image is actually there.</summary>
internal readonly record struct ResolvedImage(string Path, bool Available);

/// <summary>Finds a product's picture and says whether it exists.</summary>
internal interface IProductImages
{
    ResolvedImage Resolve(Sku sku, string imagePath);
}

/// <summary>
/// Resolves an image against the web root.
/// </summary>
/// <remarks>
/// Two rules, and they are the whole reason this is separate from mapping a record: a remote image is
/// replaced by a local copy when one has been vendored for the SKU, so the running application never
/// depends on a third-party host; and a local path counts as available only when the file is there,
/// so a page draws its placeholder rather than a broken image.
/// </remarks>
internal sealed class ProductImages(string? webRootPath) : IProductImages
{
    /// <summary>Where locally vendored product images live, relative to the web root.</summary>
    public const string VendoredDirectory = "/images/vendored";

    public ResolvedImage Resolve(Sku sku, string imagePath)
    {
        var path = VendoredFor(sku, imagePath) ?? imagePath;

        return new ResolvedImage(path, IsThere(path));
    }

    /// <summary>The vendored copy of a remote image, when one exists for this SKU.</summary>
    private string? VendoredFor(Sku sku, string imagePath)
    {
        if (!imagePath.StartsWith("http", StringComparison.OrdinalIgnoreCase)
            || string.IsNullOrWhiteSpace(webRootPath))
        {
            return null;
        }

        var directory = Path.Combine(
            webRootPath,
            VendoredDirectory.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));

        if (!Directory.Exists(directory))
        {
            return null;
        }

        foreach (var candidate in Directory.GetFiles(directory))
        {
            if (string.Equals(Path.GetFileNameWithoutExtension(candidate), sku.Value, StringComparison.OrdinalIgnoreCase))
            {
                return $"{VendoredDirectory}/{Path.GetFileName(candidate)}";
            }
        }

        return null;
    }

    /// <summary>
    /// A remote image is assumed to exist; a local path is checked against the web root when one is
    /// supplied. Anything unverifiable is reported as unavailable, so the UI draws a placeholder.
    /// </summary>
    private bool IsThere(string imagePath)
    {
        if (imagePath.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
            || imagePath.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (string.IsNullOrWhiteSpace(webRootPath))
        {
            return false;
        }

        var relativePath = imagePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
        return File.Exists(Path.Combine(webRootPath, relativePath));
    }
}
