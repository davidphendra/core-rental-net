using CoreRentalNet.BuildingBlocks.Domain;
using CoreRentalNet.Modules.Rentals.Domain.Rentals;

namespace CoreRentalNet.Modules.Rentals.Domain.Invoices;

/// <summary>
/// What is owed for one period. Every amount is frozen at issue, so a later price change cannot
/// rewrite a bill that has already been sent.
/// </summary>
/// <remarks>
/// A plain persistence object. Issuing and settling live in <c>IInvoiceService</c>.
/// </remarks>
public sealed class Invoice
{
    public InvoiceId Id { get; set; }

    public InvoiceNumber Number { get; set; } = null!;

    public RentalId RentalId { get; set; }

    /// <summary>Zero for the month the order starts in, then one, two, and so on.</summary>
    public int PeriodIndex { get; set; }

    public DateOnly PeriodStart { get; set; }

    public DateOnly PeriodEnd { get; set; }

    public List<InvoiceLine> Lines { get; set; } = [];

    public Money Subtotal { get; set; } = new Money(0m, Currencies.Idr);

    /// <summary>Stored per invoice, so a future rate change does not reinterpret an old bill.</summary>
    public decimal TaxRate { get; set; }

    public Money TaxAmount { get; set; } = new Money(0m, Currencies.Idr);

    public Money DeliveryFee { get; set; } = new Money(0m, Currencies.Idr);

    public Money Total { get; set; } = new Money(0m, Currencies.Idr);

    public InvoiceStatus Status { get; set; }

    public DateOnly IssuedOn { get; set; }

    public DateOnly? PaidOn { get; set; }

    public int Version { get; set; }
}
