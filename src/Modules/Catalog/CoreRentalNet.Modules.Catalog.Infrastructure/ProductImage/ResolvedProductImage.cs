namespace CoreRentalNet.Modules.Catalog.Infrastructure.ProductImage;

/// <summary>The path to use for a product's image, and whether that image is actually there.</summary>
internal readonly record struct ResolvedProductImage(string Path, bool Available);
