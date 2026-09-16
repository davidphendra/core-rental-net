using CoreRentalNet.BuildingBlocks.Domain;
using CoreRentalNet.Modules.Rentals.Domain.Invoices;

namespace CoreRentalNet.Modules.Rentals.Application.Queries.Views;

/// <summary>An invoice as the statement page needs it, assembled from the frozen invoice.</summary>
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
