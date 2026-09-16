
using CoreRentalNet.Modules.Rentals.Domain.Invoices;
using CoreRentalNet.Modules.Rentals.Domain.Rentals;

namespace CoreRentalNet.Modules.Rentals.Application.Services;

/// <summary>
/// Issuing and settling an invoice, moved off the <c>Invoice</c> aggregate.
/// </summary>
public interface IInvoiceService
{
    /// <summary>
    /// Issues the invoice for one period of a rental. The one-time delivery and setup charge
    /// appears on the first invoice only and never on a renewal.
    /// </summary>
    Invoice IssueFor(InvoiceId id, InvoiceNumber number, Rental rental, int periodIndex, decimal taxRate, DateOnly issuedOn);

    /// <summary>Settlement always succeeds here; there is no failure path to model.</summary>
    void Settle(Invoice invoice, DateOnly on);

    /// <summary>False while the tax rate is zero, so no empty tax line is shown.</summary>
    bool HasTaxLine(Invoice invoice);

    bool IsFirstPeriod(Invoice invoice);
}
