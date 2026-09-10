using AwesomeAssertions;
using CoreRentalNet.BuildingBlocks.Domain;
using CoreRentalNet.Modules.Rentals.Domain;
using Xunit;

namespace CoreRentalNet.Modules.Rentals.UnitTests;

public sealed class InvoiceTests
{
    private static readonly DateOnly Placed = new(2026, 1, 10);

    private static Domain.Rental Rental(decimal deliveryFee = 750_000m)
        => Domain.Rental.Place(
            RentalId.New(),
            RentalNumber.Of(2026, 1),
            AccessToken.HashOf("raw"),
            "Villa Lotus, Canggu",
            Money.Idr(deliveryFee),
            [
                new RentalLine("CHA449AGLBB0", "Seminyak Lounge", 1, Money.Idr(400_000m)),
                new RentalLine("MONJVAP81NPQ", "Batu Bolong 27\" 4K", 2, Money.Idr(300_000m)),
            ],
            Placed);

    private static Invoice Issue(Domain.Rental rental, int periodIndex = 0, decimal taxRate = 0m, int sequence = 1)
        => Invoice.IssueFor(InvoiceId.New(), InvoiceNumber.Of(2026, sequence), rental, periodIndex, taxRate, Placed);

    [Fact] // CO-06, CO-09
    public void The_first_invoice_carries_the_month_and_the_one_time_charge_with_no_tax_line()
    {
        var invoice = Issue(Rental());

        invoice.PeriodIndex.Should().Be(0);
        invoice.PeriodStart.Should().Be(Placed);
        invoice.PeriodEnd.Should().Be(new DateOnly(2026, 2, 10));
        invoice.Subtotal.Amount.Should().Be(1_000_000m);
        invoice.DeliveryFee.Amount.Should().Be(750_000m);
        invoice.TaxAmount.Amount.Should().Be(0m);
        invoice.HasTaxLine.Should().BeFalse();
        invoice.Total.Amount.Should().Be(1_750_000m);
        invoice.Status.Should().Be(InvoiceStatus.Open);
        invoice.Lines.Should().HaveCount(2);
    }

    [Fact] // CO-08
    public void A_renewal_never_carries_the_delivery_charge_again()
    {
        var rental = Rental();

        var first = Issue(rental, periodIndex: 0, sequence: 1);
        var second = Issue(rental, periodIndex: 1, sequence: 2);

        first.DeliveryFee.Amount.Should().Be(750_000m);
        second.DeliveryFee.Amount.Should().Be(0m);
        second.Total.Amount.Should().Be(1_000_000m);
        second.PeriodStart.Should().Be(new DateOnly(2026, 2, 10));
    }

    [Fact] // CO-06
    public void Invoice_lines_are_copies_of_the_order_lines()
    {
        var rental = Rental();

        var invoice = Issue(rental);

        invoice.Lines.Select(line => (line.Sku, line.Quantity, line.LineTotal.Amount)).Should().Equal(
            rental.Lines.Select(line => (line.Sku, line.Quantity, line.LineTotal.Amount)));
        invoice.Lines[1].LineTotal.Amount.Should().Be(600_000m);
    }

    [Fact] // CO-06
    public void The_invoice_total_is_assembled_from_its_own_parts()
    {
        var rental = Rental();

        var atZero = Issue(rental, taxRate: 0m, sequence: 1);
        var atEleven = Issue(rental, taxRate: 0.11m, sequence: 2);

        atEleven.TaxRate.Should().Be(0.11m);
        atEleven.TaxAmount.Amount.Should().Be(110_000m, "tax applies to the rental, not the delivery charge");
        atEleven.HasTaxLine.Should().BeTrue();
        atEleven.Total.Amount.Should().Be(1_860_000m);

        atEleven.Subtotal.Amount.Should().Be(
            atZero.Subtotal.Amount,
            "changing the rate must not rewrite the amount already invoiced");
    }

    [Fact] // CO-07
    public void Settling_marks_the_invoice_paid_and_records_when()
    {
        var invoice = Issue(Rental());

        invoice.Settle(Placed);

        invoice.Status.Should().Be(InvoiceStatus.Paid);
        invoice.PaidOn.Should().Be(Placed);
        invoice.Total.Amount.Should().Be(1_750_000m, "settling must not change what was owed");
    }

    [Fact] // CO-07
    public void An_invoice_cannot_be_paid_twice_or_paid_before_it_was_issued()
    {
        var invoice = Issue(Rental());
        invoice.Settle(Placed);

        ((Action)(() => invoice.Settle(Placed))).Should().Throw<DomainRuleViolationException>().WithMessage("*already been paid*");

        var fresh = Issue(Rental(), sequence: 2);
        ((Action)(() => fresh.Settle(Placed.AddDays(-1)))).Should().Throw<DomainRuleViolationException>();
    }

    [Theory]
    [InlineData(-0.01)]
    [InlineData(1.01)]
    public void An_impossible_tax_rate_is_refused(decimal rate)
    {
        var action = () => Issue(Rental(), taxRate: rate);

        action.Should().Throw<DomainRuleViolationException>();
    }

    [Fact]
    public void A_month_that_spans_a_month_end_is_billed_for_the_whole_month()
    {
        var rental = Domain.Rental.Place(
            RentalId.New(),
            RentalNumber.Of(2026, 3),
            AccessToken.HashOf("raw"),
            "Villa Lotus, Canggu",
            Money.Idr(750_000m),
            [new RentalLine("CHA449AGLBB0", "Seminyak Lounge", 1, Money.Idr(400_000m))],
            new DateOnly(2026, 1, 31));

        var february = Invoice.IssueFor(InvoiceId.New(), InvoiceNumber.Of(2026, 1), rental, 1, 0m, new DateOnly(2026, 1, 31));

        february.PeriodStart.Should().Be(new DateOnly(2026, 2, 28));
        february.PeriodEnd.Should().Be(new DateOnly(2026, 3, 31));
        february.Subtotal.Amount.Should().Be(400_000m, "there is no proration; a month costs a month");
    }
}
