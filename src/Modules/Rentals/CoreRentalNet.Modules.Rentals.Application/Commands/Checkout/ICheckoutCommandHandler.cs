namespace CoreRentalNet.Modules.Rentals.Application.Commands.Checkout;

/// <summary>Turns a draft into a paid order; the operation the review page asks for.</summary>
public interface ICheckoutCommandHandler
{
    Task<CheckoutResult> CheckoutAsync(CheckoutCommand command, CancellationToken cancellationToken = default);
}
