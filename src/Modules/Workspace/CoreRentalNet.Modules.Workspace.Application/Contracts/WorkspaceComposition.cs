namespace CoreRentalNet.Modules.Workspace.Application.Contracts;

public sealed record WorkspaceCompositionLine(string Sku, int Quantity);

/// <summary>
/// A frozen snapshot of a draft, handed to Rentals at checkout. Rentals never reads this
/// module's tables and never sees its Domain types.
/// </summary>
public sealed record WorkspaceComposition(
    Guid WorkspaceId,
    IReadOnlyList<WorkspaceCompositionLine> Lines,
    string? DeliveryAddress)
{
    public bool IsEmpty => Lines.Count == 0;

    public int TotalUnits => Lines.Sum(line => line.Quantity);
}
