namespace CoreRentalNet.Modules.Rentals.Application.Scheduling;

/// <summary>One thing the run did, recorded so the caller can log it and tests can assert on it.</summary>
public sealed record RentalScheduleAction(string RentalNumber, ScheduleActionKind Kind, string? InvoiceNumber = null);
