using CoreRentalNet.Modules.Workspace.Application.Queries.Views;
using CoreRentalNet.Modules.Workspace.Domain;

namespace CoreRentalNet.Host.Presentation;

/// <summary>
/// The workspace as a component sees it: what it holds, the last refusal, and the ways to change it.
/// </summary>
/// <remarks>
/// A component depends on this rather than on the session itself, so a component can be exercised
/// without a database behind it and the session's shape can change without touching markup. The
/// session is registered under this interface in the same lifetime, because the circuit's state must
/// survive exactly as it did.
/// </remarks>
public interface IWorkspaceSession
{
    WorkspaceView? Current { get; }

    string? Error { get; }

    bool IsLoaded { get; }

    bool IsEmpty { get; }

    int TotalUnits { get; }

    event Action? Changed;

    Task RefreshAsync(string draftToken, CancellationToken cancellationToken = default);

    Task EnsureLoadedAsync(string draftToken, CancellationToken cancellationToken = default);

    Task AssignAsync(string draftToken, string sku, CancellationToken cancellationToken = default);

    Task RemoveAsync(string draftToken, SlotId slot, CancellationToken cancellationToken = default);

    Task SetQuantityAsync(string draftToken, SlotId slot, string sku, int quantity, CancellationToken cancellationToken = default);

    Task SetDeliveryAddressAsync(string draftToken, string? address, CancellationToken cancellationToken = default);

    Task<string?> TrySetDeliveryAddressAsync(string draftToken, string? address, CancellationToken cancellationToken = default);

    void ClearError();
}
