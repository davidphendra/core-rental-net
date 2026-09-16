using CoreRentalNet.BuildingBlocks.Application;
using CoreRentalNet.Modules.Rentals.Application.Commands.Checkout;
using CoreRentalNet.Modules.Rentals.Application.Rules;
using CoreRentalNet.Modules.Rentals.Application.Services;
using CoreRentalNet.Modules.Rentals.Domain.Rentals;

namespace CoreRentalNet.Modules.Rentals.Application.Services;

/// <summary>The stored order as the customer last saw it, with no access token to hand back.</summary>
public sealed class CheckoutConfirmation(
    IMoneyService money,
    IRentalLifecycleService lifecycle,
    IDeliveryPolicyService deliveries) : ICheckoutConfirmation
{
    /// <inheritdoc />
    public CheckoutResult AlreadyPlaced(Rental rental)
    {
        ArgumentNullException.ThrowIfNull(rental);

        return new CheckoutResult(
            rental.Number.Value,
            string.Empty,
            string.Empty,
            money.Add(lifecycle.MonthlyTotal(rental), rental.DeliveryFee),
            rental.PlacedOn,
            rental.DeliveryScheduledFor ?? deliveries.ScheduledFor(rental.PlacedOn),
            WasAlreadyPlaced: true);
    }
}
