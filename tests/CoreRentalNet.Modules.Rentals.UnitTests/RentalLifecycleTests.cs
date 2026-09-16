using AwesomeAssertions;
using CoreRentalNet.BuildingBlocks.Domain;
using Xunit;
using CoreRentalNet.BuildingBlocks.Application;
using CoreRentalNet.Modules.Rentals.Domain.Rentals;

namespace CoreRentalNet.Modules.Rentals.UnitTests;

/// <summary>
/// The rental lifecycle, now enforced by <see cref="Application.Services.RentalLifecycleService"/>
/// against a plain record. Every assertion is the same as it was against the aggregate;
/// only the call site moved.
/// </summary>
public sealed class RentalLifecycleTests
{
    private static readonly DateOnly Placed = new(2026, 1, 31);

    private static Rental PlacedRental(DateOnly? placedOn = null)
    {
        var on = placedOn ?? Placed;

        return new Rental
        {
            Id = RentalId.New(),
            WorkspaceId = Guid.NewGuid(),
            Number = RentalNumber.Of(2026, 1),
            AccessTokenHash = new OpaqueTokenService().HashOf("raw"),
            DeliveryAddress = "Villa Lotus, Canggu",
            DeliveryFee = new Money(750_000m, Currencies.Idr),
            PlacedOn = on,
            AnchorDate = on,
            Status = RentalStatus.Placed,
            Version = 1,
            Lines =
            [
                new RentalLine { Sku = "CHA449AGLBB0", Name = "Seminyak Lounge", Quantity = 1, UnitMonthlyPrice = new Money(400_000m, Currencies.Idr) },
            ],
        };
    }

    [Fact] // CO-06
    public void A_placed_order_snapshots_its_lines_and_totals()
    {
        var rental = PlacedRental();

        rental.Status.Should().Be(RentalStatus.Placed);
        rental.Lines.Should().ContainSingle();
        RentalsTestGraph.Lifecycle.MonthlyTotal(rental).Amount.Should().Be(400_000m);
        rental.AnchorDate.Should().Be(Placed);
        rental.PlacedOn.Should().Be(Placed);
        rental.DeliveryAddress.Should().Be("Villa Lotus, Canggu");
        rental.Version.Should().Be(1);
    }

    [Fact] // SC-11
    public void The_lifecycle_only_moves_forward_through_its_named_steps()
    {
        var rental = PlacedRental();

        RentalsTestGraph.Lifecycle.MarkPaid(rental, Placed);
        rental.Status.Should().Be(RentalStatus.Paid);

        RentalsTestGraph.Lifecycle.ScheduleDelivery(rental, RentalsTestGraph.Deliveries.ScheduledFor(Placed));
        rental.Status.Should().Be(RentalStatus.DeliveryScheduled);

        RentalsTestGraph.Lifecycle.Activate(rental, RentalsTestGraph.Deliveries.ScheduledFor(Placed));
        rental.Status.Should().Be(RentalStatus.Active);
        rental.ActivatedOn.Should().Be(RentalsTestGraph.Deliveries.ScheduledFor(Placed));
    }

    [Fact] // SC-11
    public void Steps_taken_out_of_order_are_refused()
    {
        var rental = PlacedRental();

        ((Action)(() => RentalsTestGraph.Lifecycle.Activate(rental, Placed))).Should().Throw<DomainRuleViolationException>();
        ((Action)(() => RentalsTestGraph.Lifecycle.ScheduleDelivery(rental, Placed))).Should().Throw<DomainRuleViolationException>("payment comes first");
        ((Action)(() => RentalsTestGraph.Lifecycle.End(rental, Placed))).Should().Throw<DomainRuleViolationException>();
        ((Action)(() => RentalsTestGraph.Lifecycle.MarkPaid(rental, Placed.AddDays(-1)))).Should().Throw<DomainRuleViolationException>();
    }

    [Fact] // SC-06
    public void Cancelling_takes_effect_at_the_end_of_the_period_already_paid_for()
    {
        var rental = PlacedRental(new DateOnly(2026, 1, 10));
        RentalsTestGraph.Lifecycle.MarkPaid(rental, new DateOnly(2026, 1, 10));

        RentalsTestGraph.Lifecycle.RequestCancellation(rental, new DateOnly(2026, 1, 20));

        rental.Status.Should().Be(RentalStatus.CancellationRequested);
        rental.CancellationRequestedOn.Should().Be(new DateOnly(2026, 1, 20));
        rental.EndsOn.Should().Be(new DateOnly(2026, 2, 10), "the month already paid for runs to 10 February");
    }

    [Fact] // SC-06
    public void A_request_on_the_last_day_of_the_period_ends_it_the_next_day()
    {
        var rental = PlacedRental(new DateOnly(2026, 1, 10));
        RentalsTestGraph.Lifecycle.MarkPaid(rental, new DateOnly(2026, 1, 10));

        RentalsTestGraph.Lifecycle.RequestCancellation(rental, new DateOnly(2026, 2, 9));

        rental.EndsOn.Should().Be(new DateOnly(2026, 2, 10), "the last day of the paid month still belongs to the customer");
    }

    [Fact] // SC-06
    public void Ending_before_the_paid_period_is_over_is_refused()
    {
        var rental = PlacedRental(new DateOnly(2026, 1, 10));
        RentalsTestGraph.Lifecycle.MarkPaid(rental, new DateOnly(2026, 1, 10));
        RentalsTestGraph.Lifecycle.RequestCancellation(rental, new DateOnly(2026, 1, 20));

        var action = () => RentalsTestGraph.Lifecycle.End(rental, new DateOnly(2026, 2, 1));

        action.Should().Throw<DomainRuleViolationException>().WithMessage("*runs until*");
    }

    [Fact] // SC-11
    public void Cancelling_before_delivery_is_a_separate_operation_from_requesting_cancellation()
    {
        var rental = PlacedRental();
        RentalsTestGraph.Lifecycle.MarkPaid(rental, Placed);

        RentalsTestGraph.Lifecycle.CancelBeforeDelivery(rental, Placed);

        rental.Status.Should().Be(RentalStatus.Cancelled);
        ((Action)(() => RentalsTestGraph.Lifecycle.RequestCancellation(rental, Placed))).Should().Throw<DomainRuleViolationException>();
    }

    [Fact]
    public void An_ended_order_cannot_be_cancelled_again()
    {
        var rental = PlacedRental(new DateOnly(2026, 1, 10));
        RentalsTestGraph.Lifecycle.MarkPaid(rental, new DateOnly(2026, 1, 10));
        RentalsTestGraph.Lifecycle.RequestCancellation(rental, new DateOnly(2026, 1, 20));
        RentalsTestGraph.Lifecycle.End(rental, new DateOnly(2026, 2, 10));

        rental.Status.Should().Be(RentalStatus.Ended);
        ((Action)(() => RentalsTestGraph.Lifecycle.RequestCancellation(rental, new DateOnly(2026, 2, 11)))).Should().Throw<DomainRuleViolationException>();
    }

    [Fact]
    public void Every_accepted_step_bumps_the_concurrency_version()
    {
        var rental = PlacedRental();
        var start = rental.Version;

        RentalsTestGraph.Lifecycle.MarkPaid(rental, Placed);
        RentalsTestGraph.Lifecycle.ScheduleDelivery(rental, RentalsTestGraph.Deliveries.ScheduledFor(Placed));

        rental.Version.Should().Be(start + 2);
    }

    [Fact]
    public void A_delivery_cannot_be_scheduled_before_the_order_was_placed()
    {
        var rental = PlacedRental();
        RentalsTestGraph.Lifecycle.MarkPaid(rental, Placed);

        var action = () => RentalsTestGraph.Lifecycle.ScheduleDelivery(rental, Placed.AddDays(-1));

        action.Should().Throw<DomainRuleViolationException>();
    }
}
