namespace CoreRentalNet.Modules.Rentals.Application.Rules;

/// <summary>
/// Cancelling stops the next charge; it does not refund the month already paid for. The customer
/// keeps the equipment to the end of the period they have paid.
/// </summary>
public interface ICancellationPolicyService
{
    DateOnly EffectiveEnd(DateOnly anchor, DateOnly requestedOn);
}
