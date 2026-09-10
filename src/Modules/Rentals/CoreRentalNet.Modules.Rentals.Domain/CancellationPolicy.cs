namespace CoreRentalNet.Modules.Rentals.Domain;

/// <summary>
/// Cancelling stops the next charge; it does not refund the month already paid for. The customer
/// keeps the equipment to the end of the period they have paid (ADR-0011).
/// </summary>
public static class CancellationPolicy
{
    public static DateOnly EffectiveEnd(DateOnly anchor, DateOnly requestedOn)
        => RenewalPolicy.PeriodContaining(anchor, requestedOn).EndExclusive;
}
