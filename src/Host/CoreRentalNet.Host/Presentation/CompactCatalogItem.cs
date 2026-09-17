using CoreRentalNet.Modules.Catalog.Application.Contracts;

namespace CoreRentalNet.Host.Presentation;

/// <summary>
/// One product, as a compact answer carries it: what a caller matches a request against and prices it
/// with, and nothing it renders.
/// </summary>
/// <remarks>
/// The image path, the image flag and the featured flag are absent because the caller that asks for
/// this names SKUs and recomputes amounts - the application resolves pictures, and the agent has no
/// page to draw. The price is a number rather than a Money object, and the currency travels once on
/// the envelope, because the catalogue holds one currency and repeating it on every row is most of
/// what the compact projection leaves out.
/// </remarks>
public sealed record CompactCatalogItem(
    string Sku,
    string Name,
    CatalogCategory Category,
    CatalogSubCategory? SubCategory,
    decimal PricePerMonth,
    string Description,
    CatalogMetadata Metadata);
