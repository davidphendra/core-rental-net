using CoreRentalNet.BuildingBlocks.Domain;
using CoreRentalNet.Modules.Catalog.Application.Contracts;

namespace CoreRentalNet.Host.Mcp;

/// <summary>A product as the catalogue MCP tools publish it.</summary>
/// <remarks>
/// This is deliberately not <see cref="ProductDto"/>. The REST contract and the MCP contract are separate
/// boundaries: an MCP-only rule such as a bounded description must not leak into the REST answer, and a REST
/// change must not silently change what a model is shown.
/// </remarks>
public sealed record CatalogueToolProduct(
    string Sku,
    string Name,
    CatalogCategory Category,
    CatalogSubCategory? SubCategory,
    Money MonthlyPrice,
    string Description,
    string ImagePath,
    bool ImageAvailable,
    bool IsFeatured)
{
    public static CatalogueToolProduct From(ProductDto product)
        => new(
            product.Sku,
            product.Name,
            product.Category,
            product.SubCategory,
            product.MonthlyPrice,
            CatalogueDescriptionPolicy.Truncate(product.Description),
            product.ImagePath,
            product.ImageAvailable,
            product.IsFeatured);
}
