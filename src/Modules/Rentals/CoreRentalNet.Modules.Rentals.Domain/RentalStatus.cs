namespace CoreRentalNet.Modules.Rentals.Domain;

/// <summary>
/// There is no payment-failed state: payment cannot fail in this application (ADR-0010).
/// </summary>
public enum RentalStatus
{
    Placed = 1,
    Paid = 2,
    DeliveryScheduled = 3,
    Active = 4,
    CancellationRequested = 5,
    Ended = 6,
    Cancelled = 7,
}
