namespace CoreRentalNet.Modules.Rentals.Application.Commands.PlaceOrder;

/// <summary>Builds an order, issues and settles its first invoice, and saves both.</summary>
public interface IPlaceOrder
{
    Task<PlaceOrderResult> PlaceAsync(PlaceOrderRequest request, CancellationToken cancellationToken = default);
}
