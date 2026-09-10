namespace CoreRentalNet.Modules.Rentals.Application.Scheduling;

public enum ScheduleActionKind
{
    DeliveryScheduled = 1,
    Activated = 2,
    InvoiceIssued = 3,
    Ended = 4,
}

/// <summary>One thing the run did, recorded so the caller can log it and tests can assert on it.</summary>
public sealed record RentalScheduleAction(string RentalNumber, ScheduleActionKind Kind, string? InvoiceNumber = null);

/// <summary>
/// What a single time-driven pass did.
/// </summary>
/// <remarks>
/// A failure against one order is recorded rather than thrown: one order that cannot advance must
/// not stop the rest of the book from being billed or delivered.
/// </remarks>
public sealed record SchedulingOutcome(
    DateOnly AsOf,
    IReadOnlyList<RentalScheduleAction> Actions,
    IReadOnlyList<string> Failures)
{
    public int CountOf(ScheduleActionKind kind) => Actions.Count(action => action.Kind == kind);

    public bool DidNothing => Actions.Count == 0;
}
