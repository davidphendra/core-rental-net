namespace CoreRentalNet.Modules.Rentals.Application.Scheduling;

/// <summary>What one scheduler pass did to one order.</summary>
public enum ScheduleActionKind
{
    DeliveryScheduled = 1,
    Activated = 2,
    InvoiceIssued = 3,
    Ended = 4,
}
