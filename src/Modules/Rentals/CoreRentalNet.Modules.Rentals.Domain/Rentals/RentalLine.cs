using CoreRentalNet.BuildingBlocks.Domain;

namespace CoreRentalNet.Modules.Rentals.Domain.Rentals;

/// <summary>
/// A line of an order, with the price frozen at the moment it was placed. A later catalog
/// change can never rewrite what this order costs.
/// </summary>
/// <remarks>
/// A plain persistence object. The rounded line total is derived by
/// <c>IRentalLifecycleService</c> from the unit price and the quantity, so it is not stored.
/// </remarks>
public sealed class RentalLine
{
    public string Sku { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public int Quantity { get; set; }

    public Money UnitMonthlyPrice { get; set; } = new Money(0m, Currencies.Idr);
}
