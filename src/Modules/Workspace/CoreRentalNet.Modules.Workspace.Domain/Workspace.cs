namespace CoreRentalNet.Modules.Workspace.Domain;

/// <summary>
/// The customer's draft, as it is stored: the slot assignments and the delivery address.
/// </summary>
/// <remarks>
/// A plain persistence object. It holds data and no rules: what a slot accepts, how a
/// quantity changes, what an address must look like, and what makes a draft terminal are all enforced
/// by <c>IWorkspaceService</c> in the application.
/// </remarks>
public sealed class Workspace
{
    public WorkspaceId Id { get; set; }

    public string DraftTokenHash { get; set; } = string.Empty;

    public DraftState State { get; set; }

    /// <summary>
    /// Bumped by every accepted change and mapped as a concurrency token, so two tabs editing the
    /// same draft produce a conflict rather than a silent lost update.
    /// </summary>
    public int Version { get; set; }

    public string? DeliveryAddress { get; set; }

    public List<SlotAssignment> Assignments { get; set; } = [];
}
