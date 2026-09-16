namespace CoreRentalNet.Modules.Catalog.Infrastructure.ProductImage;

/// <summary>Finds a product's picture and says whether it exists.</summary>
internal interface IProductImage
{
    ResolvedProductImage Resolve(string sku, string imagePath);
}
