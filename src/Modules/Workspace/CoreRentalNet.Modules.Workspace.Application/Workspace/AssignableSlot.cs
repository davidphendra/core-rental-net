using CoreRentalNet.BuildingBlocks.Domain;
using CoreRentalNet.Modules.Workspace.Domain;

namespace CoreRentalNet.Modules.Workspace.Application.Workspace;

/// <summary>
/// One slot as the UI needs it, filled or empty. Every slot is present so the canvas can
/// draw its dashed placeholder without knowing the rules.
/// </summary>
public sealed record AssignableSlot(
    SlotId Slot,
    string DisplayName,
    int MaxQuantity,
    string? Sku,
    string? Name,
    int Quantity,
    Money? UnitMonthlyPrice,
    string? ImagePath,
    bool ImageAvailable)
{
    public bool IsFilled => Sku is not null;
}
