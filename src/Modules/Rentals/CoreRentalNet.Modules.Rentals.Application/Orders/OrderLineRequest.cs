using CoreRentalNet.BuildingBlocks.Domain;

namespace CoreRentalNet.Modules.Rentals.Application.Orders;

/// <summary>One line of a frozen workspace composition, priced by the catalog.</summary>
public sealed record OrderLineRequest(string Sku, string Name, int Quantity, Money UnitMonthlyPrice);
