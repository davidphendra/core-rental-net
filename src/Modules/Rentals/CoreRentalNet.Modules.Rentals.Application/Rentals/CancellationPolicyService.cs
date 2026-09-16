namespace CoreRentalNet.Modules.Rentals.Application.Rentals;

/// <summary>
/// Cancelling stops the next charge; it does not refund the month already paid for.
/// Moved off the Domain's static policy.
/// </summary>
public sealed class CancellationPolicyService(IRenewalPolicyService renewals) : ICancellationPolicyService
{
    public DateOnly EffectiveEnd(DateOnly anchor, DateOnly requestedOn)
        => renewals.PeriodContaining(anchor, requestedOn).EndExclusive;
}
