using CoreRentalNet.BuildingBlocks.Domain;
using CoreRentalNet.Modules.Rentals.Domain;

namespace CoreRentalNet.Modules.Rentals.Application.Views;

public sealed record InvoiceLineView(
    string Sku,
    string Name,
    int Quantity,
    Money UnitMonthlyPrice,
    Money LineTotal);

public sealed record InvoiceView(
    string Number,
    int PeriodIndex,
    DateOnly PeriodStart,
    DateOnly PeriodEnd,
    Money Subtotal,
    decimal TaxRate,
    Money TaxAmount,
    bool HasTaxLine,
    Money DeliveryFee,
    Money Total,
    InvoiceStatus Status,
    DateOnly IssuedOn,
    DateOnly? PaidOn,
    IReadOnlyList<InvoiceLineView> Lines);
