using CoreRentalNet.Modules.Catalog.Infrastructure.ProductImage;

namespace CoreRentalNet.Modules.Catalog.UnitTests;

/// <summary>An image resolver whose answer the test chooses, so image side effects stay out of it.</summary>
internal sealed class StubProductImage(ResolvedProductImage answer) : IProductImage
{
    public ResolvedProductImage Resolve(string sku, string imagePath) => answer;
}
