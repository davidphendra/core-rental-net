using CoreRentalNet.BuildingBlocks.Domain;

namespace CoreRentalNet.Modules.Rentals.Application.Views;

/// <summary>One line of an invoice, as the statement page shows it.</summary>
public sealed record InvoiceLineView(
    string Sku,
    string Name,
    int Quantity,
    Money UnitMonthlyPrice,
    Money LineTotal);
