using CoreRentalNet.Modules.Catalog.Application.Contracts;

namespace CoreRentalNet.Host.Presentation;

/// <summary>
/// One product, as a compact answer carries it: what a caller matches a request against and prices it
/// with, and nothing it renders.
/// </summary>
/// <remarks>
/// The image path, the image flag and the featured flag are absent because the caller that asks for this
/// names SKUs and recomputes amounts - the application resolves pictures, and the agent has no page to draw.
/// The price is a number and its currency travels beside it, so a row read on its own still says what its
/// price is in; the envelope states the one currency the catalogue is priced in as well, because an answer
/// with no rows has no row to read it from.
/// </remarks>
public sealed record CompactCatalogItem(
    string Sku,
    CatalogCategory Category,
    string Name,
    CatalogSubCategory? SubCategory,
    string Description,
    decimal PricePerMonth,
    string Currency);
