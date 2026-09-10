using CoreRentalNet.BuildingBlocks.Domain;
using CoreRentalNet.Modules.Catalog.Domain;
using CoreRentalNet.Modules.Catalog.Infrastructure.Loading;

namespace CoreRentalNet.Modules.Catalog.UnitTests;

internal static class SampleCatalog
{
    public static Product Chair(string sku, decimal price, ProductBadge? badge = null)
        => new(Sku.Of(sku), "Chair " + sku, ProductCategory.Chair, null, Money.Idr(price), "A chair.", "/images/chair.svg", badge, true);

    public static Product Desk(string sku, decimal price, ProductBadge? badge = null)
        => new(Sku.Of(sku), "Desk " + sku, ProductCategory.Desk, null, Money.Idr(price), "A desk.", "/images/desk.svg", badge, true);

    public static Product Accessory(string sku, decimal price, ProductSubCategory subCategory, ProductBadge? badge = null)
        => new(Sku.Of(sku), "Accessory " + sku, ProductCategory.Accessory, subCategory, Money.Idr(price), "An accessory.", "/images/accessory.svg", badge, true);

    public static ProductCatalogSnapshot Snapshot() => new(
    [
        Chair("CHA0001", 400000m, ProductBadge.Popular),
        Desk("DSK0001", 800000m),
        Accessory("MON0001", 300000m, ProductSubCategory.Monitor),
        Accessory("LMP0001", 120000m, ProductSubCategory.Lamp),
        Accessory("PLT0001", 200000m, ProductSubCategory.Plant, ProductBadge.Popular),
        Accessory("CFE0001", 750000m, ProductSubCategory.Coffee),
        Accessory("BBG0001", 350000m, ProductSubCategory.Beanbag),
    ]);
}
