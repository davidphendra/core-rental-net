namespace CoreRentalNet.Modules.Workspace.Domain;

/// <summary>One slot holding a number of units of a single product. Holds no price.</summary>
/// <remarks>
/// A plain persistence object. The bounds on <see cref="Quantity"/> are enforced by
/// <c>IWorkspaceService</c>, which is the only thing that writes an assignment.
/// </remarks>
public sealed class SlotAssignment
{
    public SlotId Slot { get; set; }

    public string Sku { get; set; } = string.Empty;

    public int Quantity { get; set; }
}
