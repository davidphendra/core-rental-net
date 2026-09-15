using CoreRentalNet.BuildingBlocks.Domain;
using CoreRentalNet.Modules.Workspace.Domain;

namespace CoreRentalNet.Modules.Workspace.Application.Queries.Views;

/// <summary>
/// One priced line. <see cref="UnitMonthlyPrice"/> is null when the catalog no longer knows
/// the SKU, which is how an unresolvable reference is reported instead of throwing or
/// silently disappearing from the total (matrix WS-13).
/// </summary>
public sealed record QuoteLine(
    SlotId Slot,
    string Sku,
    string Name,
    int Quantity,
    Money? UnitMonthlyPrice,
    Money? LineMonthlyTotal,
    string? ImagePath,
    bool ImageAvailable)
{
    public bool IsAvailable => UnitMonthlyPrice is not null;
}
