using CoreRentalNet.BuildingBlocks.Application;
using CoreRentalNet.BuildingBlocks.Domain;
using CoreRentalNet.Modules.Rentals.Application.Rentals;
using CoreRentalNet.Modules.Rentals.Domain.Invoices;
using CoreRentalNet.Modules.Rentals.Domain.Rentals;

namespace CoreRentalNet.Modules.Rentals.Application.Invoicing;

/// <summary>Issues and settles invoices; the invoice itself is a plain record.</summary>
public sealed class InvoiceService(
    IMoneyService money,
    IRenewalPolicyService renewals,
    IRentalLifecycleService lifecycle) : IInvoiceService
{
    public Invoice IssueFor(
        InvoiceId id,
        InvoiceNumber number,
        Rental rental,
        int periodIndex,
        decimal taxRate,
        DateOnly issuedOn)
    {
        ArgumentNullException.ThrowIfNull(rental);

        if (taxRate is < 0m or > 1m)
        {
            throw new DomainRuleViolationException($"A tax rate is a fraction between 0 and 1, but {taxRate} was given.");
        }

        var period = renewals.For(rental.AnchorDate, periodIndex);
        var subtotal = lifecycle.MonthlyTotal(rental);

        var lines = rental.Lines.Select(line => new InvoiceLine
        {
            Sku = line.Sku,
            Name = line.Name,
            Quantity = line.Quantity,
            UnitMonthlyPrice = line.UnitMonthlyPrice,
            LineTotal = money.Round(money.Times(line.UnitMonthlyPrice, line.Quantity)),
        }).ToList();

        var deliveryFee = periodIndex == 0 ? rental.DeliveryFee: new Money(0m, Currencies.Idr);

        // Tax applies to the rental itself, not to the one-time delivery charge.
        var taxAmount = new Money(money.RoundAmount(subtotal.Amount * taxRate), Currencies.Idr);

        return new Invoice
        {
            Id = id,
            Number = number,
            RentalId = rental.Id,
            PeriodIndex = periodIndex,
            PeriodStart = period.Start,
            PeriodEnd = period.EndExclusive,
            Lines = lines,
            Subtotal = subtotal,
            TaxRate = taxRate,
            TaxAmount = taxAmount,
            DeliveryFee = deliveryFee,
            Total = money.Add(money.Add(subtotal, deliveryFee), taxAmount),
            Status = InvoiceStatus.Open,
            IssuedOn = issuedOn,
            Version = 1,
        };
    }

    public void Settle(Invoice invoice, DateOnly on)
    {
        ArgumentNullException.ThrowIfNull(invoice);

        if (invoice.Status == InvoiceStatus.Paid)
        {
            throw new DomainRuleViolationException($"Invoice {invoice.Number} has already been paid.");
        }

        if (on < invoice.IssuedOn)
        {
            throw new DomainRuleViolationException($"Invoice {invoice.Number} cannot be paid before it was issued ({invoice.IssuedOn}).");
        }

        invoice.Status = InvoiceStatus.Paid;
        invoice.PaidOn = on;
        Touch(invoice);
    }

    public bool HasTaxLine(Invoice invoice)
    {
        ArgumentNullException.ThrowIfNull(invoice);

        return invoice.TaxAmount.Amount > 0m;
    }

    public bool IsFirstPeriod(Invoice invoice)
    {
        ArgumentNullException.ThrowIfNull(invoice);

        return invoice.PeriodIndex == 0;
    }

    private static void Touch(Invoice invoice) => invoice.Version++;
}
