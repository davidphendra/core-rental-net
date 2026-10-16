using CoreRentalNet.BuildingBlocks.Domain;

namespace CoreRentalNet.Modules.Catalog.Application.Contracts;

/// <summary>A product as a caller reads it: what it is, where it belongs, and what it costs.</summary>
/// <remarks>
/// The SKU is a plain string and the vocabulary is this module's published one, so a caller never
/// reaches into <c>Catalog.Domain</c>. The search metadata is deliberately absent: only the ingestion
/// tool reads it, and it lives in the store rather than in the published answer.
/// </remarks>
public sealed record ProductDto(
    string Sku,
    string Name,
    CatalogCategory Category,
    CatalogSubCategory? SubCategory,
    Money MonthlyPrice,
    string Description,
    string ImagePath,
    bool ImageAvailable,
    bool IsFeatured);
