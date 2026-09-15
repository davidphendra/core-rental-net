using CoreRentalNet.Modules.Workspace.Domain;

namespace CoreRentalNet.Modules.Workspace.Application.Queries.Views;

/// <summary>
/// One slot as the UI needs it, filled or empty. Every slot is present so the canvas can draw its
/// dashed placeholder without knowing the rules.
/// </summary>
/// <remarks>
/// A slot that accepts several units carries one item per product rather than one product with a
/// count, because the three monitors a workspace may hold need not be the same monitor.
/// </remarks>
public sealed record AssignableSlot(
    SlotId Slot,
    string DisplayName,
    int MaxQuantity,
    IReadOnlyList<AssignableItem> Items)
{
    public bool IsFilled => Items.Count > 0;

    /// <summary>How many units the slot holds altogether.</summary>
    public int Quantity => Items.Sum(item => item.Quantity);

    public bool Holds(string sku) => Items.Any(item => item.Sku == sku);
}
