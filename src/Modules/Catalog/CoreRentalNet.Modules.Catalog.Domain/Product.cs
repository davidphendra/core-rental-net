using CoreRentalNet.BuildingBlocks.Domain;

namespace CoreRentalNet.Modules.Catalog.Domain;

/// <summary>
/// A rentable item, as it is stored in <c>products.json</c>.
/// </summary>
/// <remarks>
/// A plain record: it carries data and no business process. The rules about what a
/// catalogue record has to say live in the loader that reads the file, so a product cannot be
/// constructed with an invalid shape only because the loader refused it. <c>IsFeatured</c> is a
/// plain flag: the file either carries the popular badge or it does not.
/// </remarks>
public sealed record Product(
    string Sku,
    string Name,
    ProductCategory Category,
    ProductSubCategory? SubCategory,
    Money MonthlyPrice,
    string Description,
    ProductMetadata Metadata,
    string ImagePath,
    bool IsFeatured,
    bool ImageAvailable);
