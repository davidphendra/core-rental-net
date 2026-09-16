namespace CoreRentalNet.Modules.Rentals.Application.Commands.PlaceOrder;

/// <summary>
/// Everything the order needs. Note what is absent: no price total, no number and no status.
/// The caller supplies what was rented and where it goes, and this module decides the rest.
/// </summary>
public sealed record PlaceOrderRequest(
    Guid WorkspaceId,
    IReadOnlyList<OrderLineRequest> Lines,
    string DeliveryAddress);
