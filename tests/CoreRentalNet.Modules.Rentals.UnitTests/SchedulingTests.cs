using AwesomeAssertions;
using CoreRentalNet.BuildingBlocks.Domain;
using CoreRentalNet.Modules.Rentals.Application;
using CoreRentalNet.Modules.Rentals.Application.Orders;
using CoreRentalNet.Modules.Rentals.Application.Scheduling;
using CoreRentalNet.Modules.Rentals.Domain;
using Microsoft.Extensions.Time.Testing;
using Xunit;

namespace CoreRentalNet.Modules.Rentals.UnitTests;

/// <summary>
/// The order in which time moves an order forward. Every test moves a clock; none of them waits.
/// </summary>
public sealed class SchedulingTests
{
    private static readonly DateTimeOffset PlacedAt = new(2026, 1, 10, 6, 0, 0, TimeSpan.Zero);

    private sealed class Fixture
    {
        public Fixture()
        {
            Clock = new FakeTimeProvider(PlacedAt);
            Rentals = new InMemoryRentalRepository();
            Invoices = new InMemoryInvoiceRepository();
            Numbers = new CountingNumberSequence();
            UnitOfWork = new RecordingUnitOfWork();
            Settings = new RentalsSettings(Money.Idr(750_000m));
            Scheduler = new RentalScheduler(Rentals, Invoices, Numbers, UnitOfWork, Settings);
            PlaceOrder = new PlaceOrderService(Rentals, Invoices, Numbers, UnitOfWork, Settings, Clock);
        }

        public FakeTimeProvider Clock { get; }

        public InMemoryRentalRepository Rentals { get; }

        public InMemoryInvoiceRepository Invoices { get; }

        public CountingNumberSequence Numbers { get; }

        public RecordingUnitOfWork UnitOfWork { get; }

        public RentalsSettings Settings { get; }

        public RentalScheduler Scheduler { get; }

        public PlaceOrderService PlaceOrder { get; }

        public async Task<PlaceOrderResult> PlaceAsync(decimal months = 1)
        {
            var lines = new List<OrderLineRequest>
            {
                new("CHA449AGLBB0", "Seminyak Lounge", 1, Money.Idr(400_000m)),
            };

            return await PlaceOrder.PlaceAsync(new PlaceOrderRequest(Guid.NewGuid(), lines, "Villa Lotus, Canggu"));
        }

        /// <summary>Moves the clock to a date, as the business calendar sees it.</summary>
        public void MoveTo(int year, int month, int day) => Clock.SetUtcNow(new DateTimeOffset(year, month, day, 6, 0, 0, TimeSpan.Zero));

        public Domain.Rental TheRental => Rentals.All.Single();

        public Task<SchedulingOutcome> RunAsync(DateOnly asOf) => Scheduler.RunOnceAsync(asOf);
    }

    [Fact] // SC-01
    public async Task A_paid_order_is_scheduled_for_delivery_at_the_lead_time()
    {
        var fixture = new Fixture();
        await fixture.PlaceAsync();

        var outcome = await fixture.RunAsync(new DateOnly(2026, 1, 10));

        outcome.CountOf(ScheduleActionKind.DeliveryScheduled).Should().Be(1);
        fixture.TheRental.Status.Should().Be(RentalStatus.DeliveryScheduled);
        fixture.TheRental.DeliveryScheduledFor.Should().Be(new DateOnly(2026, 1, 12), "two days after the order");
    }

    [Fact] // SC-02
    public async Task The_order_becomes_active_on_the_delivery_date_and_not_before()
    {
        var fixture = new Fixture();
        await fixture.PlaceAsync();
        await fixture.RunAsync(new DateOnly(2026, 1, 10));

        var early = await fixture.RunAsync(new DateOnly(2026, 1, 11));
        early.CountOf(ScheduleActionKind.Activated).Should().Be(0);
        fixture.TheRental.Status.Should().Be(RentalStatus.DeliveryScheduled);

        var onTheDay = await fixture.RunAsync(new DateOnly(2026, 1, 12));
        onTheDay.CountOf(ScheduleActionKind.Activated).Should().Be(1);
        fixture.TheRental.Status.Should().Be(RentalStatus.Active);
        fixture.TheRental.ActivatedOn.Should().Be(new DateOnly(2026, 1, 12));
    }

    [Fact] // SC-03
    public async Task A_renewal_is_raised_and_settled_when_the_next_month_begins()
    {
        var fixture = new Fixture();
        await fixture.PlaceAsync();
        await fixture.RunAsync(new DateOnly(2026, 1, 12)); // now live

        var outcome = await fixture.RunAsync(new DateOnly(2026, 2, 11));

        outcome.CountOf(ScheduleActionKind.InvoiceIssued).Should().Be(1);
        fixture.Invoices.All.Should().HaveCount(2);

        var renewal = fixture.Invoices.All.Single(invoice => invoice.PeriodIndex == 1);
        renewal.PeriodStart.Should().Be(new DateOnly(2026, 2, 10));
        renewal.Status.Should().Be(InvoiceStatus.Paid, "every payment succeeds in this application");
        renewal.PaidOn.Should().Be(new DateOnly(2026, 2, 11));
        renewal.DeliveryFee.Amount.Should().Be(0m, "the delivery charge belongs to the first month only");
        renewal.Total.Amount.Should().Be(400_000m);
    }

    [Fact] // SC-04
    public async Task Running_twice_for_the_same_period_raises_one_invoice()
    {
        var fixture = new Fixture();
        await fixture.PlaceAsync();
        await fixture.RunAsync(new DateOnly(2026, 1, 12));

        await fixture.RunAsync(new DateOnly(2026, 2, 11));
        var second = await fixture.RunAsync(new DateOnly(2026, 2, 11));
        var third = await fixture.RunAsync(new DateOnly(2026, 2, 20));

        second.CountOf(ScheduleActionKind.InvoiceIssued).Should().Be(0);
        third.CountOf(ScheduleActionKind.InvoiceIssued).Should().Be(0);
        fixture.Invoices.All.Should().HaveCount(2, "one for the month paid at checkout and one renewal");
    }

    [Fact] // SC-04
    public async Task The_very_first_run_never_duplicates_the_month_paid_at_checkout()
    {
        var fixture = new Fixture();
        await fixture.PlaceAsync();

        await fixture.RunAsync(new DateOnly(2026, 1, 10));
        await fixture.RunAsync(new DateOnly(2026, 1, 12));
        await fixture.RunAsync(new DateOnly(2026, 1, 20));

        fixture.Invoices.All.Should().HaveCount(1);
        fixture.Invoices.All.Single().PeriodIndex.Should().Be(0);
    }

    [Fact] // SC-04
    public async Task A_gap_catches_up_one_invoice_per_period_that_started()
    {
        var fixture = new Fixture();
        await fixture.PlaceAsync();
        await fixture.RunAsync(new DateOnly(2026, 1, 12));

        // Nothing runs for three months, then the service comes back.
        var outcome = await fixture.RunAsync(new DateOnly(2026, 5, 1));

        outcome.CountOf(ScheduleActionKind.InvoiceIssued).Should().Be(3, "February, March and April each began");
        fixture.Invoices.All.Select(invoice => invoice.PeriodIndex).Should().Equal(0, 1, 2, 3);
        fixture.Invoices.All.Should().OnlyContain(invoice => invoice.Status == InvoiceStatus.Paid);
        fixture.TheRental.AnchorDate.Should().Be(new DateOnly(2026, 1, 10), "the anchor never moves");
    }

    [Fact] // SC-05
    public async Task Cancelling_suppresses_the_next_renewal()
    {
        var fixture = new Fixture();
        await fixture.PlaceAsync();
        await fixture.RunAsync(new DateOnly(2026, 1, 12));

        fixture.TheRental.RequestCancellation(new DateOnly(2026, 1, 20));

        var outcome = await fixture.RunAsync(new DateOnly(2026, 2, 20));

        outcome.CountOf(ScheduleActionKind.InvoiceIssued).Should().Be(0);
        fixture.Invoices.All.Should().HaveCount(1, "the month already paid for is the last one");
        fixture.TheRental.EndsOn.Should().Be(new DateOnly(2026, 2, 10));
    }

    [Fact] // SC-06
    public async Task The_order_ends_when_the_paid_month_is_over_and_not_before()
    {
        var fixture = new Fixture();
        await fixture.PlaceAsync();
        await fixture.RunAsync(new DateOnly(2026, 1, 12));
        fixture.TheRental.RequestCancellation(new DateOnly(2026, 1, 20));

        var early = await fixture.RunAsync(new DateOnly(2026, 2, 9));
        early.CountOf(ScheduleActionKind.Ended).Should().Be(0);
        fixture.TheRental.Status.Should().Be(RentalStatus.CancellationRequested);

        var onTheDay = await fixture.RunAsync(new DateOnly(2026, 2, 10));
        onTheDay.CountOf(ScheduleActionKind.Ended).Should().Be(1);
        fixture.TheRental.Status.Should().Be(RentalStatus.Ended);
    }

    [Fact] // SC-05, SC-06
    public async Task A_cancelled_order_never_catches_up_periods_past_its_end()
    {
        var fixture = new Fixture();
        await fixture.PlaceAsync();
        await fixture.RunAsync(new DateOnly(2026, 1, 12));
        fixture.TheRental.RequestCancellation(new DateOnly(2026, 1, 20));
        await fixture.RunAsync(new DateOnly(2026, 2, 10)); // ended

        var later = await fixture.RunAsync(new DateOnly(2026, 6, 1));

        later.Actions.Should().BeEmpty();
        fixture.Invoices.All.Should().HaveCount(1);
    }

    [Fact] // SC-10
    public async Task A_pass_changes_nothing_when_there_is_nothing_to_do()
    {
        var fixture = new Fixture();
        await fixture.PlaceAsync();
        await fixture.RunAsync(new DateOnly(2026, 1, 12));

        var versionBefore = fixture.TheRental.Version;
        var invoicesBefore = fixture.Invoices.All.Count;
        var outcome = await fixture.RunAsync(new DateOnly(2026, 1, 20));

        outcome.DidNothing.Should().BeTrue();
        outcome.Failures.Should().BeEmpty();
        fixture.TheRental.Version.Should().Be(versionBefore, "nothing about the order may change");
        fixture.Invoices.All.Should().HaveCount(invoicesBefore, "and nothing may be billed");

        // Note: the pass still commits. An earlier version skipped the save when the order itself
        // had not changed, which silently dropped renewals, because billing does not touch the
        // order. Asserting "no write happened" was asserting the bug.
    }

    [Fact] // SC-11
    public async Task The_whole_life_of_an_order_can_be_driven_by_time_alone()
    {
        var fixture = new Fixture();
        await fixture.PlaceAsync();
        fixture.TheRental.Status.Should().Be(RentalStatus.Paid);

        // The run that follows payment takes it all the way to live, because the delivery date has
        // already passed by the time this first pass happens.
        await fixture.RunAsync(new DateOnly(2026, 1, 12));
        fixture.TheRental.Status.Should().Be(RentalStatus.Active);

        await fixture.RunAsync(new DateOnly(2026, 2, 12));
        fixture.TheRental.RequestCancellation(new DateOnly(2026, 2, 15));

        await fixture.RunAsync(new DateOnly(2026, 3, 12));

        fixture.TheRental.Status.Should().Be(RentalStatus.Ended);
        fixture.Invoices.All.Should().HaveCount(2, "January and February were paid for; March was not");
    }

    [Fact] // SC-11
    public async Task An_order_placed_on_the_thirty_first_renews_on_the_thirty_first_when_there_is_one()
    {
        var fixture = new Fixture();
        var placed = await fixture.PlaceAsync();
        placed.RentalNumber.Should().Be("CR-2026-0001");

        // Move the whole fixture to a 31 January order by placing another one on that date.
        fixture.MoveTo(2026, 1, 31);
        var second = await fixture.PlaceAsync();
        var rental = fixture.Rentals.All.Single(candidate => candidate.Number.Value == second.RentalNumber);

        await fixture.RunAsync(new DateOnly(2026, 1, 31));
        await fixture.RunAsync(new DateOnly(2026, 3, 5));

        var periods = fixture.Invoices.All
            .Where(invoice => invoice.RentalId == rental.Id)
            .OrderBy(invoice => invoice.PeriodIndex)
            .Select(invoice => invoice.PeriodStart)
            .ToArray();

        periods.Should().Equal(new DateOnly(2026, 1, 31), new DateOnly(2026, 2, 28));
    }

    [Fact]
    public async Task One_order_that_cannot_advance_does_not_stop_the_others()
    {
        var fixture = new Fixture();
        await fixture.PlaceAsync();
        var healthy = fixture.TheRental;

        // A second order that is already cancelled outright, so a further step is impossible.
        var broken = Domain.Rental.Place(
            RentalId.New(),
            Guid.NewGuid(),
            RentalNumber.Of(2026, 99),
            AccessToken.HashOf("other"),
            "Villa Lotus, Canggu",
            Money.Idr(750_000m),
            [new RentalLine("CHA449AGLBB0", "Seminyak Lounge", 1, Money.Idr(400_000m))],
            new DateOnly(2026, 1, 10));
        broken.MarkPaid(new DateOnly(2026, 1, 10));
        broken.CancelBeforeDelivery(new DateOnly(2026, 1, 10));
        fixture.Rentals.Seed(broken);

        var outcome = await fixture.RunAsync(new DateOnly(2026, 1, 12));

        outcome.Failures.Should().BeEmpty("a cancelled order simply has nothing to do");
        healthy.Status.Should().Be(RentalStatus.Active);
    }

    [Fact]
    public async Task A_finished_order_is_left_alone()
    {
        var fixture = new Fixture();
        await fixture.PlaceAsync();
        await fixture.RunAsync(new DateOnly(2026, 1, 12));
        fixture.TheRental.RequestCancellation(new DateOnly(2026, 1, 20));
        await fixture.RunAsync(new DateOnly(2026, 2, 10));

        fixture.TheRental.Status.Should().Be(RentalStatus.Ended);
        var version = fixture.TheRental.Version;

        var outcome = await fixture.RunAsync(new DateOnly(2026, 3, 1));

        outcome.DidNothing.Should().BeTrue();
        fixture.TheRental.Version.Should().Be(version);
        fixture.Invoices.All.Should().HaveCount(1);
    }
}
