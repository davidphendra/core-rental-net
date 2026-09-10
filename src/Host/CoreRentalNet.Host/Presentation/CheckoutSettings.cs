using CoreRentalNet.BuildingBlocks.Domain;

namespace CoreRentalNet.Host.Presentation;

/// <summary>
/// Provisional home for the one-time delivery and setup charge.
/// </summary>
/// <remarks>
/// The charge is an order concern, so it moves into the invoice model when the Rentals module
/// lands. Until then it is read from configuration rather than written into markup, so the
/// number stays in one place and the amount never becomes a literal in a component.
/// </remarks>
public sealed class CheckoutSettings
{
    public CheckoutSettings(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        DeliveryFee = Money.Idr(configuration.GetValue("Checkout:DeliveryFeeAmount", 750_000m));
    }

    public Money DeliveryFee { get; }
}
