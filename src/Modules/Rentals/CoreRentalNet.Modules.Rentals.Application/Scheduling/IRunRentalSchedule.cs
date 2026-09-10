namespace CoreRentalNet.Modules.Rentals.Application.Scheduling;

/// <summary>
/// Advances every order that time has moved on. Takes the date rather than reading a clock, so the
/// whole thing is deterministic under test.
/// </summary>
public interface IRunRentalSchedule
{
    Task<SchedulingOutcome> RunOnceAsync(DateOnly asOf, CancellationToken cancellationToken = default);
}
