using AwesomeAssertions;
using CoreRentalNet.BuildingBlocks.Domain;
using CoreRentalNet.Modules.Rentals.Application;
using CoreRentalNet.Modules.Rentals.Application.Orders;
using CoreRentalNet.Modules.Rentals.Application.Queries.GetRentalByToken;
using CoreRentalNet.Modules.Rentals.Application.Queries.GetInvoicesByToken;
using Microsoft.Extensions.Time.Testing;
using Xunit;
using CoreRentalNet.BuildingBlocks.Application;
using CoreRentalNet.Modules.Rentals.Domain.Invoices;
using CoreRentalNet.Modules.Rentals.Domain.Rentals;

namespace CoreRentalNet.Modules.Rentals.UnitTests;

public sealed class PlaceOrderTests
{
    private sealed class Fixture
    {
        public Fixture(DateOnly? today = null)
        {
            var date = today ?? new DateOnly(2026, 1, 10);
            Clock = new FakeTimeProvider(new DateTimeOffset(date.Year, date.Month, date.Day, 6, 0, 0, TimeSpan.Zero));
            Rentals = new InMemoryRentalRepository();
            Invoices = new InMemoryInvoiceRepository();
            Numbers = new CountingNumberSequence();
            UnitOfWork = new RecordingUnitOfWork();
            Settings = new RentalsSettings(new Money(750_000m, Currencies.Idr));
            Service = new PlaceOrderService(
                Rentals, Invoices, RentalsTestGraph.Tokens, RentalsTestGraph.Invoicing, RentalsTestGraph.Lifecycle,
                RentalsTestGraph.Deliveries, Numbers, UnitOfWork, Settings, Clock);
        }

        public FakeTimeProvider Clock { get; }

        public InMemoryRentalRepository Rentals { get; }

        public InMemoryInvoiceRepository Invoices { get; }

        public CountingNumberSequence Numbers { get; }

        public RecordingUnitOfWork UnitOfWork { get; }

        public RentalsSettings Settings { get; }

        public PlaceOrderService Service { get; }

        public static PlaceOrderRequest Request(params OrderLineRequest[] lines)
            => new(
                Guid.NewGuid(),
                lines.Length == 0
                    ? [new OrderLineRequest("CHA449AGLBB0", "Seminyak Lounge", 1, new Money(400_000m, Currencies.Idr))]
                    : lines,
                "Villa Lotus, Canggu");
    }

    [Theory] // ADDR-01
    [InlineData("")]
    [InlineData("   ")]
    public async Task An_order_without_a_delivery_address_is_refused(string address)
    {
        var fixture = new Fixture();

        var placing = async () => await fixture.Service.PlaceAsync(Fixture.Request() with { DeliveryAddress = address });

        await placing.Should().ThrowAsync<DomainRuleViolationException>().WithMessage("*delivery address*");
    }

    [Fact]
    public async Task An_order_without_a_workspace_is_refused()
    {
        // The guard used to sit on Rental.Place; it lives in PlaceOrderService now.
        var fixture = new Fixture();

        var placing = async () => await fixture.Service.PlaceAsync(Fixture.Request() with { WorkspaceId = Guid.Empty });

        await placing.Should().ThrowAsync<DomainRuleViolationException>()
            .WithMessage("An order must remember the workspace it came from.");
    }

    [Fact]
    public async Task A_line_with_no_units_is_refused()
    {
        // The guard used to sit on the RentalLine constructor; it lives in PlaceOrderService now.
        var fixture = new Fixture();

        var placing = async () => await fixture.Service.PlaceAsync(new PlaceOrderRequest(
            Guid.NewGuid(),
            [new OrderLineRequest("CHA449AGLBB0", "Chair", 0, new Money(400_000m, Currencies.Idr))],
            "Villa Lotus, Canggu"));

        await placing.Should().ThrowAsync<DomainRuleViolationException>()
            .WithMessage("A line needs at least one unit, but 0 was given.");
    }

    [Fact] // CO-06, CO-07
    public async Task Placing_an_order_creates_it_and_settles_the_first_month()
    {
        var fixture = new Fixture();

        var result = await fixture.Service.PlaceAsync(Fixture.Request());

        fixture.Rentals.All.Should().ContainSingle();
        fixture.Invoices.All.Should().ContainSingle();

        var rental = fixture.Rentals.All[0];
        var invoice = fixture.Invoices.All[0];

        rental.Status.Should().Be(RentalStatus.Paid);
        invoice.Status.Should().Be(InvoiceStatus.Paid);
        invoice.RentalId.Should().Be(rental.Id);
        invoice.PeriodIndex.Should().Be(0);
        result.FirstInvoiceTotal.Amount.Should().Be(1_150_000m, "400,000 of chair plus the 750,000 delivery charge");
        result.InvoiceNumber.Should().Be(invoice.Number.Value);
        result.RentalNumber.Should().Be(rental.Number.Value);
    }

    [Fact] // CO-14
    public async Task Numbers_are_reserved_for_both_the_order_and_the_invoice()
    {
        var fixture = new Fixture();

        var first = await fixture.Service.PlaceAsync(Fixture.Request());
        var second = await fixture.Service.PlaceAsync(Fixture.Request());

        first.RentalNumber.Should().Be("CR-2026-0001");
        second.RentalNumber.Should().Be("CR-2026-0002");
        first.InvoiceNumber.Should().Be("INV-2026-0001");
        second.InvoiceNumber.Should().Be("INV-2026-0002");
        fixture.Numbers.Reservations.Should().Be(4);
    }

    [Fact] // SEC-02
    public async Task Only_the_hash_of_the_access_token_is_stored()
    {
        var fixture = new Fixture();

        var result = await fixture.Service.PlaceAsync(Fixture.Request());

        var rental = fixture.Rentals.All[0];
        rental.AccessTokenHash.Should().NotBe(result.RawAccessToken);
        rental.AccessTokenHash.Should().Be(new OpaqueTokenService().HashOf(result.RawAccessToken));
        rental.AccessTokenHash.Should().HaveLength(64);
    }

    [Fact] // SC-01
    public async Task Delivery_is_scheduled_a_fixed_lead_time_after_the_order()
    {
        var fixture = new Fixture();

        var result = await fixture.Service.PlaceAsync(Fixture.Request());

        result.PlacedOn.Should().Be(new DateOnly(2026, 1, 10));
        result.DeliveryScheduledFor.Should().Be(new DateOnly(2026, 1, 12));
    }

    [Fact] // SC-09
    public async Task The_order_date_is_the_bali_date_not_the_utc_one()
    {
        // 19:00 UTC on 31 January is 03:00 on 1 February in Denpasar.
        var clock = new FakeTimeProvider(new DateTimeOffset(2026, 1, 31, 19, 0, 0, TimeSpan.Zero));
        var fixture = new Fixture();
        var service = new PlaceOrderService(
            fixture.Rentals, fixture.Invoices, RentalsTestGraph.Tokens, RentalsTestGraph.Invoicing, RentalsTestGraph.Lifecycle,
            RentalsTestGraph.Deliveries, fixture.Numbers, fixture.UnitOfWork, fixture.Settings, clock);

        var result = await service.PlaceAsync(Fixture.Request());

        result.PlacedOn.Should().Be(new DateOnly(2026, 2, 1));
        result.RentalNumber.Should().Be("CR-2026-0001");
        fixture.Rentals.All[0].AnchorDate.Should().Be(new DateOnly(2026, 2, 1));
    }

    [Fact]
    public async Task Everything_is_saved_once_in_a_single_unit_of_work()
    {
        var fixture = new Fixture();

        await fixture.Service.PlaceAsync(Fixture.Request());

        fixture.UnitOfWork.Saves.Should().Be(1);
    }

    [Fact]
    public async Task An_order_with_no_lines_is_refused_before_anything_is_written()
    {
        var fixture = new Fixture();

        var action = async () => await fixture.Service.PlaceAsync(
            new PlaceOrderRequest(Guid.NewGuid(), [], "Villa Lotus, Canggu"));

        await action.Should().ThrowAsync<DomainRuleViolationException>();
        fixture.UnitOfWork.Saves.Should().Be(0);
        fixture.Numbers.Reservations.Should().Be(0, "nothing should be reserved for an order that was never placed");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task An_order_with_no_address_is_refused(string address)
    {
        var fixture = new Fixture();

        var action = async () => await fixture.Service.PlaceAsync(new PlaceOrderRequest(
            Guid.NewGuid(),
            [new OrderLineRequest("CHA449AGLBB0", "Seminyak Lounge", 1, new Money(400_000m, Currencies.Idr))],
            address));

        await action.Should().ThrowAsync<DomainRuleViolationException>();
    }

    [Fact]
    public async Task A_quantity_is_multiplied_into_the_frozen_line_total()
    {
        var fixture = new Fixture();

        var result = await fixture.Service.PlaceAsync(Fixture.Request(
            new OrderLineRequest("MONJVAP81NPQ", "Batu Bolong 27\" 4K", 3, new Money(300_000m, Currencies.Idr))));

        RentalsTestGraph.Lifecycle.MonthlyTotal(fixture.Rentals.All[0]).Amount.Should().Be(900_000m);
        result.FirstInvoiceTotal.Amount.Should().Be(1_650_000m);
    }

    [Fact] // ORD-01
    public async Task An_order_can_be_read_back_with_its_token()
    {
        var fixture = new Fixture();
        var result = await fixture.Service.PlaceAsync(Fixture.Request());
        var handler = new GetRentalByTokenHandler(fixture.Rentals, RentalsTestGraph.Tokens, RentalsTestGraph.Money, RentalsTestGraph.Lifecycle, fixture.Clock);

        var view = await handler.HandleAsync(new GetRentalByTokenQuery(result.RawAccessToken));

        view.Should().NotBeNull();
        view!.Number.Should().Be(result.RentalNumber);
        view.Status.Should().Be(RentalStatus.Paid);
        view.DeliveryAddress.Should().Be("Villa Lotus, Canggu");
        view.MonthlyTotal.Amount.Should().Be(400_000m);
        view.DeliveryFee.Amount.Should().Be(750_000m);
        view.Lines.Should().ContainSingle();
        view.CurrentPeriodStart.Should().Be(new DateOnly(2026, 1, 10));
        view.CurrentPeriodEnd.Should().Be(new DateOnly(2026, 2, 10));
    }

    [Fact] // ORD-02, SEC-06
    public async Task An_unknown_token_discloses_nothing()
    {
        var fixture = new Fixture();
        await fixture.Service.PlaceAsync(Fixture.Request());
        var rentalHandler = new GetRentalByTokenHandler(fixture.Rentals, RentalsTestGraph.Tokens, RentalsTestGraph.Money, RentalsTestGraph.Lifecycle, fixture.Clock);
        var invoiceHandler = new GetInvoicesByTokenHandler(fixture.Rentals, fixture.Invoices, RentalsTestGraph.Tokens, RentalsTestGraph.Invoicing);

        (await rentalHandler.HandleAsync(new GetRentalByTokenQuery("guessed-token"))).Should().BeNull();
        (await rentalHandler.HandleAsync(new GetRentalByTokenQuery("  "))).Should().BeNull();
        (await invoiceHandler.HandleAsync(new GetInvoicesByTokenQuery("guessed-token"))).Should().BeEmpty();
    }

    [Fact] // ORD-04
    public async Task The_confirmation_can_be_reopened_and_shows_the_same_order()
    {
        var fixture = new Fixture();
        var result = await fixture.Service.PlaceAsync(Fixture.Request());
        var handler = new GetRentalByTokenHandler(fixture.Rentals, RentalsTestGraph.Tokens, RentalsTestGraph.Money, RentalsTestGraph.Lifecycle, fixture.Clock);

        var first = await handler.HandleAsync(new GetRentalByTokenQuery(result.RawAccessToken));
        var second = await handler.HandleAsync(new GetRentalByTokenQuery(result.RawAccessToken));

        second.Should().BeEquivalentTo(first, "reopening the link must show the same order");
    }

    [Fact] // ORD-03
    public async Task The_first_invoice_is_readable_by_token()
    {
        var fixture = new Fixture();
        var result = await fixture.Service.PlaceAsync(Fixture.Request());
        var handler = new GetInvoicesByTokenHandler(fixture.Rentals, fixture.Invoices, RentalsTestGraph.Tokens, RentalsTestGraph.Invoicing);

        var invoices = await handler.HandleAsync(new GetInvoicesByTokenQuery(result.RawAccessToken));

        invoices.Should().ContainSingle();
        invoices[0].Number.Should().Be(result.InvoiceNumber);
        invoices[0].PeriodIndex.Should().Be(0);
        invoices[0].HasTaxLine.Should().BeFalse();
        invoices[0].DeliveryFee.Amount.Should().Be(750_000m);
        invoices[0].Status.Should().Be(InvoiceStatus.Paid);
        invoices[0].PaidOn.Should().Be(new DateOnly(2026, 1, 10));
    }
}
