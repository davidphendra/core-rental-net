using CoreRentalNet.BuildingBlocks.Domain;

namespace CoreRentalNet.Modules.Rentals.Application.Queries.Views;

/// <summary>One line of an order, as the confirmation page shows it.</summary>
public sealed record RentalLineView(
    string Sku,
    string Name,
    int Quantity,
    Money UnitMonthlyPrice,
    Money LineTotal);
