using CoreRentalNet.Modules.Workspace.Domain;

namespace CoreRentalNet.Modules.Workspace.Application.Services;

/// <summary>
/// Every change to a draft goes through this service, and it is the one place that enforces the
/// slot table, the address bounds and the terminal state.
/// </summary>
/// <remarks>
/// The workspace itself is a persistence record and holds none of this. Every accepted change bumps
/// <see cref="Workspace.Version"/> here, which is now the service's job rather than the record's.
/// The rule-free projections live in <see cref="IWorkspaceQueryService"/>.
/// </remarks>
public interface IWorkspaceService
{
    void Assign(Domain.Workspace workspace, SlotId slot, string sku, int quantity = 1);

    /// <summary>Empties a slot. Returns false when the slot was already empty.</summary>
    bool Remove(Domain.Workspace workspace, SlotId slot);

    /// <summary>Sets how many of one product a slot holds. Zero removes that product.</summary>
    void ChangeQuantity(Domain.Workspace workspace, SlotId slot, string sku, int quantity);

    void SetDeliveryAddress(Domain.Workspace workspace, string? address);

    /// <summary>Makes the draft terminal. Refuses a second conversion.</summary>
    void MarkConverted(Domain.Workspace workspace);
}
