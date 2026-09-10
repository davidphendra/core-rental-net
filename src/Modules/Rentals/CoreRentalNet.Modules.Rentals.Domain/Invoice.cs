using CoreRentalNet.BuildingBlocks.Domain;

namespace CoreRentalNet.Modules.Rentals.Domain;

/// <summary>
/// What is owed for one period. Every amount is frozen at issue, so a later price change cannot
/// rewrite a bill that has already been sent.
/// </summary>
public sealed class Invoice
{
    private readonly List<InvoiceLine> lines = [];

    private Invoice()
    {
        Number = null!;
        Subtotal = Money.Idr(0m);
        TaxAmount = Money.Idr(0m);
        DeliveryFee = Money.Idr(0m);
        Total = Money.Idr(0m);
    }

    private Invoice(
        InvoiceId id,
        InvoiceNumber number,
        RentalId rentalId,
        int periodIndex,
        RentalPeriod period,
        IReadOnlyList<InvoiceLine> lines,
        Money subtotal,
        decimal taxRate,
        Money taxAmount,
        Money deliveryFee,
        DateOnly issuedOn)
    {
        Id = InvoiceId.From(id.Value);
        Number = number ?? throw new DomainRuleViolationException("An invoice requires a number.");
        RentalId = RentalId.From(rentalId.Value);
        PeriodIndex = periodIndex;
        PeriodStart = period.Start;
        PeriodEnd = period.EndExclusive;
        this.lines.AddRange(lines);
        Subtotal = subtotal;
        TaxRate = taxRate;
        TaxAmount = taxAmount;
        DeliveryFee = deliveryFee;
        Total = subtotal.Add(deliveryFee).Add(taxAmount);
        Status = InvoiceStatus.Open;
        IssuedOn = issuedOn;
        Version = 1;
    }

    public InvoiceId Id { get; private set; }

    public InvoiceNumber Number { get; private set; }

    public RentalId RentalId { get; private set; }

    /// <summary>Zero for the month the order starts in, then one, two, and so on.</summary>
    public int PeriodIndex { get; private set; }

    public DateOnly PeriodStart { get; private set; }

    public DateOnly PeriodEnd { get; private set; }

    public IReadOnlyList<InvoiceLine> Lines => lines;

    public Money Subtotal { get; private set; }

    /// <summary>Stored per invoice, so a future rate change does not reinterpret an old bill.</summary>
    public decimal TaxRate { get; private set; }

    public Money TaxAmount { get; private set; }

    public Money DeliveryFee { get; private set; }

    public Money Total { get; private set; }

    public InvoiceStatus Status { get; private set; }

    public DateOnly IssuedOn { get; private set; }

    public DateOnly? PaidOn { get; private set; }

    public int Version { get; private set; }

    public bool IsFirstPeriod => PeriodIndex == 0;

    /// <summary>False while the tax rate is zero, so no empty tax line is shown.</summary>
    public bool HasTaxLine => TaxAmount.Amount > 0m;

    /// <summary>
    /// Issues the invoice for one period of a rental. The one-time delivery and setup charge
    /// appears on the first invoice only and never on a renewal (ADR-0011).
    /// </summary>
    public static Invoice IssueFor(
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

        var period = RentalPeriod.For(rental.AnchorDate, periodIndex);
        var subtotal = rental.MonthlyTotal;

        var lines = rental.Lines
            .Select(line => new InvoiceLine(line.Sku, line.Name, line.Quantity, line.UnitMonthlyPrice, line.LineTotal))
            .ToArray();

        var deliveryFee = periodIndex == 0 ? rental.DeliveryFee : Money.Idr(0m);

        // Tax applies to the rental itself, not to the one-time delivery charge.
        var taxAmount = Money.Idr(Money.RoundAmount(subtotal.Amount * taxRate));

        return new Invoice(id, number, rental.Id, periodIndex, period, lines, subtotal, taxRate, taxAmount, deliveryFee, issuedOn);
    }

    /// <summary>Settlement always succeeds here; there is no failure path to model (ADR-0010).</summary>
    public void Settle(DateOnly on)
    {
        if (Status == InvoiceStatus.Paid)
        {
            throw new DomainRuleViolationException($"Invoice {Number} has already been paid.");
        }

        if (on < IssuedOn)
        {
            throw new DomainRuleViolationException($"Invoice {Number} cannot be paid before it was issued ({IssuedOn}).");
        }

        Status = InvoiceStatus.Paid;
        PaidOn = on;
        Touch();
    }

    private void Touch() => Version++;
}
