using CoreRentalNet.BuildingBlocks.Domain;

namespace CoreRentalNet.Modules.Workspace.Application.Queries.Views;

/// <summary>
/// One product in a slot, as the UI needs it. It carries no prices of its own: the quote has them,
/// and the UI shows what the quote says.
/// </summary>
public sealed record AssignableItem(
    string Sku,
    string Name,
    int Quantity,
    Money? UnitMonthlyPrice,
    string? ImagePath,
    bool ImageAvailable)
{
    /// <summary>False when the product has left the catalog since it was assigned.</summary>
    public bool IsAvailable => UnitMonthlyPrice is not null;
}
