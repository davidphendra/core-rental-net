using CoreRentalNet.Modules.Catalog.Application.Catalog;
using CoreRentalNet.Modules.Catalog.Application.Contracts;
using CoreRentalNet.Modules.Workspace.Application.Catalog;
using CoreRentalNet.Modules.Workspace.Domain;

namespace CoreRentalNet.Host.Presentation;

/// <summary>Answers "what could go in this slot?" using the same total mapping the domain uses.</summary>
public static class SlotCatalog
{
    public static IReadOnlyList<ProductListItem> ForSlot(IEnumerable<ProductListItem> products, SlotId slot)
        => products
            .Where(product => CatalogSlotMapping.SlotFor(product.Category, product.SubCategory) == slot)
            .ToArray();
}
