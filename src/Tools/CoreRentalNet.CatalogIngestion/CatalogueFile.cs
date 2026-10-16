using CoreRentalNet.Modules.Catalog.Domain;
using CoreRentalNet.Modules.Catalog.Infrastructure.Loader;
using ProductImageResolver = CoreRentalNet.Modules.Catalog.Infrastructure.ProductImage.ProductImage;

namespace CoreRentalNet.CatalogIngestion;

/// <summary>The catalogue file, read the way the module itself reads it.</summary>
/// <remarks>
/// <para>
/// <b>The loader is reused rather than reimplemented.</b> It is what enforces the file's shape — the
/// category vocabulary, the metadata a row must carry, the accessory rule — so a second reader in this tool
/// would be a second definition of a valid row, and the one that drifted would be the one nothing tested.
/// </para>
/// <para>
/// <b>The web root is absent on purpose.</b> This tool never draws an image, so resolving one against a
/// deployment's files would only let image resolution fail for a reason that does not matter here.
/// </para>
/// </remarks>
internal static class CatalogueFile
{
    /// <summary>Every product the file holds, or a failure naming what is wrong with it.</summary>
    public static IReadOnlyList<Product> Read(string path)
        => ProductLoader.Read(path, new ProductImageResolver(webRootPath: null));
}
