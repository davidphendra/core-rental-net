using AwesomeAssertions;
using CoreRentalNet.BuildingBlocks.Domain;
using CoreRentalNet.Modules.Rentals.Application;
using CoreRentalNet.Modules.Rentals.Application.Commands.PlaceOrder;
using CoreRentalNet.Modules.Rentals.Application.Scheduling;
using CoreRentalNet.Modules.Rentals.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Time.Testing;
using Xunit;
using CoreRentalNet.Modules.Rentals.Domain.Invoices;
using CoreRentalNet.Modules.Rentals.Domain.Rentals;

namespace CoreRentalNet.IntegrationTests;

/// <summary>
/// Renewals against a real database, because "exactly once" is a claim about storage, not about
/// an object in memory.
/// </summary>
public sealed class RenewalSchedulingTests
{
    private static FakeTimeProvider Clock() => new(new DateTimeOffset(2026, 1, 10, 6, 0, 0, TimeSpan.Zero));

    private static async Task<(RentalsContext Context, RentalScheduler Scheduler, string Token)> ArrangeAsync(
        SqliteTestDatabase database,
        FakeTimeProvider clock)
    {
        var context = await database.CreateMigratedRentalsContextAsync();
        var rentals = new RentalRepository(context);
        var invoices = new InvoiceRepository(context);
        var numbers = new SqliteNumberSequence(context);
        var unitOfWork = new RentalsUnitOfWork(context);
        var settings = new RentalsSettings(new Money(750_000m, Currencies.Idr));

        var placed = await new PlaceOrderService(
            rentals, invoices, RentalsGraph.Tokens, RentalsGraph.Invoicing, RentalsGraph.Lifecycle, RentalsGraph.Deliveries,
            numbers, unitOfWork, settings, clock)
            .PlaceAsync(new PlaceOrderRequest(
                Guid.NewGuid(),
                [new OrderLineRequest("CHA449AGLBB0", "Seminyak Lounge", 1, new Money(400_000m, Currencies.Idr))],
                "Villa Lotus, Canggu"));

        return (context, new RentalScheduler(
            rentals, RentalsGraph.Lifecycle, RentalsGraph.Deliveries,
            new RenewalInvoiceIssuer(invoices, RentalsGraph.Lifecycle, RentalsGraph.Invoicing, RentalsGraph.Renewals, numbers, settings),
            unitOfWork), placed.RawAccessToken);
    }

    [Fact] // SC-03
    public async Task A_renewal_is_written_once_when_the_next_month_begins()
    {
        await using var database = new SqliteTestDatabase();
        var clock = Clock();
        var (context, scheduler, _) = await ArrangeAsync(database, clock);

        await scheduler.RunOnceAsync(new DateOnly(2026, 1, 12));
        var outcome = await scheduler.RunOnceAsync(new DateOnly(2026, 2, 11));

        outcome.Failures.Should().BeEmpty();
        outcome.CountOf(ScheduleActionKind.InvoiceIssued).Should().Be(1);

        context.ChangeTracker.Clear();
        var invoices = await context.Invoices.OrderBy(invoice => invoice.PeriodIndex).ToListAsync();

        invoices.Should().HaveCount(2);
        invoices[1].PeriodIndex.Should().Be(1);
        invoices[1].PeriodStart.Should().Be(new DateOnly(2026, 2, 10));
        invoices[1].DeliveryFee.Amount.Should().Be(0m);
        invoices[1].Status.Should().Be(InvoiceStatus.Paid);
        invoices[1].Number.Value.Should().Be("INV-2026-0002");
    }

    [Fact] // SC-04
    public async Task Repeating_the_pass_never_produces_a_second_invoice_for_a_period()
    {
        await using var database = new SqliteTestDatabase();
        var clock = Clock();
        var (context, scheduler, _) = await ArrangeAsync(database, clock);

        await scheduler.RunOnceAsync(new DateOnly(2026, 1, 12));

        for (var run = 0; run < 5; run++)
        {
            (await scheduler.RunOnceAsync(new DateOnly(2026, 2, 11))).Failures.Should().BeEmpty();
        }

        (await scheduler.RunOnceAsync(new DateOnly(2026, 2, 28))).Failures.Should().BeEmpty();

        context.ChangeTracker.Clear();
        (await context.Invoices.CountAsync()).Should().Be(2);
        (await context.Invoices.Select(invoice => invoice.PeriodIndex).ToListAsync()).Should().OnlyHaveUniqueItems();
    }

    [Fact] // SC-04
    public async Task The_database_itself_refuses_a_second_invoice_for_a_period()
    {
        await using var database = new SqliteTestDatabase();
        var clock = Clock();
        var (context, scheduler, _) = await ArrangeAsync(database, clock);
        await scheduler.RunOnceAsync(new DateOnly(2026, 1, 12));

        context.ChangeTracker.Clear();
        var rental = await context.Rentals.SingleAsync();

        var duplicate = RentalsGraph.Invoicing.IssueFor(
            InvoiceId.New(), InvoiceNumber.Of(2026, 500), rental, periodIndex: 0, taxRate: 0m, new DateOnly(2026, 1, 10));

        await new InvoiceRepository(context).AddAsync(duplicate);

        var action = async () => await context.SaveChangesAsync();

        await action.Should().ThrowAsync<DbUpdateException>(
            "the unique index on rental and period is the backstop under the scheduler's own check");
    }

    [Fact] // SC-05, SC-06
    public async Task Cancelling_stops_the_renewals_and_ends_the_order_at_the_paid_boundary()
    {
        await using var database = new SqliteTestDatabase();
        var clock = Clock();
        var (context, scheduler, _) = await ArrangeAsync(database, clock);

        await scheduler.RunOnceAsync(new DateOnly(2026, 1, 12));

        context.ChangeTracker.Clear();
        var rental = await context.Rentals.SingleAsync();
        RentalsGraph.Lifecycle.RequestCancellation(rental, new DateOnly(2026, 1, 20));
        await context.SaveChangesAsync();

        var afterEnd = await scheduler.RunOnceAsync(new DateOnly(2026, 3, 1));

        afterEnd.CountOf(ScheduleActionKind.InvoiceIssued).Should().Be(0, "no period began before the end date");
        afterEnd.CountOf(ScheduleActionKind.Ended).Should().Be(1);

        context.ChangeTracker.Clear();
        (await context.Invoices.CountAsync()).Should().Be(1);
        (await context.Rentals.SingleAsync()).Status.Should().Be(RentalStatus.Ended);
    }

    [Fact] // SC-05
    public async Task A_period_that_has_already_been_paid_for_is_not_billed_again_when_cancelling()
    {
        await using var database = new SqliteTestDatabase();
        var clock = Clock();
        var (context, scheduler, _) = await ArrangeAsync(database, clock);
        await scheduler.RunOnceAsync(new DateOnly(2026, 1, 12));

        context.ChangeTracker.Clear();
        var rental = await context.Rentals.SingleAsync();
        RentalsGraph.Lifecycle.RequestCancellation(rental, new DateOnly(2026, 2, 5));
        await context.SaveChangesAsync();

        var outcome = await scheduler.RunOnceAsync(new DateOnly(2026, 2, 20));

        outcome.CountOf(ScheduleActionKind.InvoiceIssued).Should().Be(0);
        context.ChangeTracker.Clear();
        (await context.Invoices.CountAsync()).Should().Be(1);
        rental.EndsOn.Should().Be(new DateOnly(2026, 2, 10));
    }
}
