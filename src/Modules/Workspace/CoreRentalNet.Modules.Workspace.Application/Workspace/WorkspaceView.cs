using CoreRentalNet.Modules.Workspace.Domain;

namespace CoreRentalNet.Modules.Workspace.Application.Workspace;

public sealed record WorkspaceView(
    Guid WorkspaceId,
    DraftState State,
    int Version,
    IReadOnlyList<AssignableSlot> Slots,
    string? DeliveryAddress,
    int TotalUnits,
    bool IsEmpty,
    WorkspaceQuote Quote)
{
    public bool CanCheckout => !IsEmpty && !Quote.HasUnavailableLines;
}
