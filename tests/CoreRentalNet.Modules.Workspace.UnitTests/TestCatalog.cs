using CoreRentalNet.BuildingBlocks.Domain;
using CoreRentalNet.Modules.Catalog.Application.Contracts;

namespace CoreRentalNet.Modules.Workspace.UnitTests;

/// <summary>A hand-written price source. No mocking library is used anywhere in this project.</summary>
internal sealed class TestCatalog : IDefineProductPrices
{
    private readonly Dictionary<string, ProductPriceView> views = new(StringComparer.OrdinalIgnoreCase);

    public TestCatalog Add(
        string sku,
        decimal monthlyPrice,
        CatalogCategory category = CatalogCategory.Accessory,
        CatalogSubCategory? subCategory = CatalogSubCategory.Monitor,
        string? name = null,
        bool imageAvailable = true)
    {
        views[sku] = new ProductPriceView(
            sku.ToUpperInvariant(),
            name ?? $"Product {sku.ToUpperInvariant()}",
            category,
            subCategory,
            Money.Idr(monthlyPrice),
            "/images/test.svg",
            imageAvailable);

        return this;
    }

    public TestCatalog Remove(string sku)
    {
        views.Remove(sku);
        return this;
    }

    public ProductPriceView? FindPrice(string sku)
        => views.TryGetValue(sku, out var view) ? view : null;

    public IReadOnlyList<ProductPriceView> AllPrices() => views.Values.ToArray();
}
