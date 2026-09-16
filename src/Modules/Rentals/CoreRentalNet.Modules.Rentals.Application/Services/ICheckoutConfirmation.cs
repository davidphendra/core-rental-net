using CoreRentalNet.Modules.Rentals.Application.Commands.Checkout;
using CoreRentalNet.Modules.Rentals.Domain.Rentals;

namespace CoreRentalNet.Modules.Rentals.Application.Services;

/// <summary>
/// Rebuilds the confirmation of an order that was already placed, so a repeated checkout can answer
/// with the same result instead of writing a second order.
/// </summary>
/// <remarks>
/// The raw access token exists once and is never stored, so the rebuilt confirmation carries the
/// identity and the money facts and an empty token. This is a projection over a stored order, kept
/// out of the checkout use case so that use case stays a coordinator of one transaction.
/// </remarks>
public interface ICheckoutConfirmation
{
    CheckoutResult AlreadyPlaced(Rental rental);
}
