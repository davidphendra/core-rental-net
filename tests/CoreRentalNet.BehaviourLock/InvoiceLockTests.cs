using AwesomeAssertions;
using CoreRentalNet.BuildingBlocks.Application;
using CoreRentalNet.BuildingBlocks.Domain;
using CoreRentalNet.Modules.Rentals.Application.Invoicing;
using CoreRentalNet.Modules.Rentals.Application.Rentals;
using Xunit;
using Invoice = CoreRentalNet.Modules.Rentals.Domain.Invoices.Invoice;
using Rental = CoreRentalNet.Modules.Rentals.Domain.Rentals.Rental;
using CoreRentalNet.Modules.Rentals.Domain.Invoices;
using CoreRentalNet.Modules.Rentals.Domain.Rentals;

namespace CoreRentalNet.BehaviourLock;

/// <summary>
/// Stage-0 behaviour lock for invoicing: what is charged, and the two refusals.
/// </summary>
/// <remarks>
/// Repointed, not relaxed, when issuing and settling moved from the <c>Invoice</c> aggregate to
/// <see cref="IInvoiceService"/> in stage 4. The old constructor guard "A line needs at least one
/// unit" was subsumed by the workspace's slot rules and the place-order guard, and is asserted there.
/// </remarks>
public sealed class InvoiceLockTests
{
    private static readonly DateOnly Placed = new(2026, 1, 31);

    private static readonly IRenewalPolicyService Renewals = new RenewalPolicyService();
    private static readonly IDeliveryPolicyService Deliveries = new DeliveryPolicyService();
    private static readonly ICancellationPolicyService Cancellations = new CancellationPolicyService(Renewals);
    private static readonly IRentalLifecycleService Lifecycle =
        new RentalLifecycleService(new MoneyService(), Renewals, Deliveries, Cancellations);
    private static readonly IInvoiceService Invoicing = new InvoiceService(new MoneyService(), Renewals, Lifecycle);

    private static Rental PlacedRental() => new()
    {
        Id = RentalId.New(),
        WorkspaceId = Guid.NewGuid(),
        Number = RentalNumber.Of(2026, 1),
        AccessTokenHash = new OpaqueTokenService().HashOf("behaviour-lock"),
        DeliveryAddress = "Villa Lotus, Canggu",
        DeliveryFee = new Money(750_000m, Currencies.Idr),
        PlacedOn = Placed,
        AnchorDate = Placed,
        Status = RentalStatus.Placed,
        Version = 1,
        Lines = [new RentalLine { Sku = "CHA449AGLBB0", Name = "Seminyak Lounge", Quantity = 1, UnitMonthlyPrice = new Money(400_000m, Currencies.Idr) }],
    };

    private static Invoice FirstInvoice(Rental rental, decimal taxRate = 0m)
        => Invoicing.IssueFor(InvoiceId.New(), InvoiceNumber.Of(2026, 1), rental, periodIndex: 0, taxRate, Placed);

    [Fact]
    public void The_first_invoice_carries_the_one_time_delivery_charge_and_totals_its_parts()
    {
        var invoice = FirstInvoice(PlacedRental());

        invoice.Subtotal.Amount.Should().Be(400_000m);
        invoice.DeliveryFee.Amount.Should().Be(750_000m);
        invoice.Total.Amount.Should().Be(1_150_000m, "subtotal + delivery fee + tax");
    }

    [Fact]
    public void A_renewal_never_carries_the_delivery_charge_again()
    {
        var rental = PlacedRental();

        var renewal = Invoicing.IssueFor(
            InvoiceId.New(), InvoiceNumber.Of(2026, 2), rental, periodIndex: 1, taxRate: 0m, Placed);

        renewal.DeliveryFee.Amount.Should().Be(0m);
        renewal.Total.Amount.Should().Be(400_000m);
        Invoicing.IsFirstPeriod(renewal).Should().BeFalse();
    }

    [Fact]
    public void An_invoice_cannot_be_paid_twice()
    {
        var invoice = FirstInvoice(PlacedRental());
        Invoicing.Settle(invoice, Placed);

        var act = () => Invoicing.Settle(invoice, Placed);

        act.Should().Throw<DomainRuleViolationException>()
            .WithMessage("Invoice INV-2026-0001 has already been paid.");
    }

    [Fact]
    public void An_invoice_cannot_be_paid_before_it_was_issued()
    {
        var invoice = FirstInvoice(PlacedRental());

        var act = () => Invoicing.Settle(invoice, Placed.AddDays(-1));

        act.Should().Throw<DomainRuleViolationException>()
            .WithMessage("Invoice INV-2026-0001 cannot be paid before it was issued (*).");
    }

    [Fact]
    public void A_tax_rate_outside_zero_to_one_is_refused()
    {
        var act = () => FirstInvoice(PlacedRental(), taxRate: 2m);

        act.Should().Throw<DomainRuleViolationException>()
            .WithMessage("A tax rate is a fraction between 0 and 1, but 2 was given.");
    }

    [Fact]
    public void Tax_is_rounded_once_from_the_rental_subtotal()
    {
        var invoice = FirstInvoice(PlacedRental(), taxRate: 0.11m);

        invoice.TaxAmount.Amount.Should().Be(44_000m, "400000 x 0.11");
        invoice.Total.Amount.Should().Be(1_194_000m);
    }
}
