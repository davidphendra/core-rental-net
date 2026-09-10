using AwesomeAssertions;
using CoreRentalNet.BuildingBlocks.Domain;
using CoreRentalNet.Modules.Rentals.Domain;
using Xunit;

namespace CoreRentalNet.Modules.Rentals.UnitTests;

public sealed class RentalLifecycleTests
{
    private static readonly DateOnly Placed = new(2026, 1, 31);

    private static Domain.Rental PlacedRental(DateOnly? placedOn = null)
        => Domain.Rental.Place(
            RentalId.New(),
            Guid.NewGuid(),
            RentalNumber.Of(2026, 1),
            AccessToken.HashOf("raw"),
            "Villa Lotus, Canggu",
            Money.Idr(750_000m),
            [new RentalLine("CHA449AGLBB0", "Seminyak Lounge", 1, Money.Idr(400_000m))],
            placedOn ?? Placed);

    [Fact] // CO-06
    public void A_placed_order_snapshots_its_lines_and_totals()
    {
        var rental = PlacedRental();

        rental.Status.Should().Be(RentalStatus.Placed);
        rental.Lines.Should().ContainSingle();
        rental.MonthlyTotal.Amount.Should().Be(400_000m);
        rental.AnchorDate.Should().Be(Placed);
        rental.PlacedOn.Should().Be(Placed);
        rental.DeliveryAddress.Should().Be("Villa Lotus, Canggu");
        rental.Version.Should().Be(1);
    }

    [Fact]
    public void An_order_without_lines_or_an_address_is_refused()
    {
        var noLines = () => Domain.Rental.Place(
            RentalId.New(), Guid.NewGuid(), RentalNumber.Of(2026, 1), AccessToken.HashOf("raw"), "Villa", Money.Idr(1m), [], Placed);
        noLines.Should().Throw<DomainRuleViolationException>();

        var noAddress = () => Domain.Rental.Place(
            RentalId.New(), Guid.NewGuid(), RentalNumber.Of(2026, 1), AccessToken.HashOf("raw"), "   ", Money.Idr(1m),
            [new RentalLine("CHA449AGLBB0", "Chair", 1, Money.Idr(400_000m))], Placed);
        noAddress.Should().Throw<DomainRuleViolationException>();
    }

    [Fact] // SC-11
    public void The_lifecycle_only_moves_forward_through_its_named_steps()
    {
        var rental = PlacedRental();

        rental.MarkPaid(Placed);
        rental.Status.Should().Be(RentalStatus.Paid);

        rental.ScheduleDelivery(DeliveryPolicy.ScheduledFor(Placed));
        rental.Status.Should().Be(RentalStatus.DeliveryScheduled);

        rental.Activate(DeliveryPolicy.ScheduledFor(Placed));
        rental.Status.Should().Be(RentalStatus.Active);
        rental.ActivatedOn.Should().Be(DeliveryPolicy.ScheduledFor(Placed));
    }

    [Fact] // SC-11
    public void Steps_taken_out_of_order_are_refused()
    {
        var rental = PlacedRental();

        ((Action)(() => rental.Activate(Placed))).Should().Throw<DomainRuleViolationException>();
        ((Action)(() => rental.ScheduleDelivery(Placed))).Should().Throw<DomainRuleViolationException>("payment comes first");
        ((Action)(() => rental.End(Placed))).Should().Throw<DomainRuleViolationException>();
        ((Action)(() => rental.MarkPaid(Placed.AddDays(-1)))).Should().Throw<DomainRuleViolationException>();
    }

    [Fact] // SC-06
    public void Cancelling_takes_effect_at_the_end_of_the_period_already_paid_for()
    {
        var rental = PlacedRental(new DateOnly(2026, 1, 10));
        rental.MarkPaid(new DateOnly(2026, 1, 10));

        rental.RequestCancellation(new DateOnly(2026, 1, 20));

        rental.Status.Should().Be(RentalStatus.CancellationRequested);
        rental.CancellationRequestedOn.Should().Be(new DateOnly(2026, 1, 20));
        rental.EndsOn.Should().Be(new DateOnly(2026, 2, 10), "the month already paid for runs to 10 February");
    }

    [Fact] // SC-06
    public void A_request_on_the_last_day_of_the_period_ends_it_the_next_day()
    {
        var rental = PlacedRental(new DateOnly(2026, 1, 10));
        rental.MarkPaid(new DateOnly(2026, 1, 10));

        rental.RequestCancellation(new DateOnly(2026, 2, 9));

        rental.EndsOn.Should().Be(new DateOnly(2026, 2, 10), "the last day of the paid month still belongs to the customer");
    }

    [Fact] // SC-06
    public void Ending_before_the_paid_period_is_over_is_refused()
    {
        var rental = PlacedRental(new DateOnly(2026, 1, 10));
        rental.MarkPaid(new DateOnly(2026, 1, 10));
        rental.RequestCancellation(new DateOnly(2026, 1, 20));

        var action = () => rental.End(new DateOnly(2026, 2, 1));

        action.Should().Throw<DomainRuleViolationException>().WithMessage("*runs until*");
    }

    [Fact] // SC-11
    public void Cancelling_before_delivery_is_a_separate_operation_from_requesting_cancellation()
    {
        var rental = PlacedRental();
        rental.MarkPaid(Placed);

        rental.CancelBeforeDelivery(Placed);

        rental.Status.Should().Be(RentalStatus.Cancelled);
        ((Action)(() => rental.RequestCancellation(Placed))).Should().Throw<DomainRuleViolationException>();
    }

    [Fact]
    public void An_ended_order_cannot_be_cancelled_again()
    {
        var rental = PlacedRental(new DateOnly(2026, 1, 10));
        rental.MarkPaid(new DateOnly(2026, 1, 10));
        rental.RequestCancellation(new DateOnly(2026, 1, 20));
        rental.End(new DateOnly(2026, 2, 10));

        rental.Status.Should().Be(RentalStatus.Ended);
        ((Action)(() => rental.RequestCancellation(new DateOnly(2026, 2, 11)))).Should().Throw<DomainRuleViolationException>();
    }

    [Fact]
    public void Every_accepted_step_bumps_the_concurrency_version()
    {
        var rental = PlacedRental();
        var start = rental.Version;

        rental.MarkPaid(Placed);
        rental.ScheduleDelivery(DeliveryPolicy.ScheduledFor(Placed));

        rental.Version.Should().Be(start + 2);
    }

    [Fact]
    public void A_delivery_cannot_be_scheduled_before_the_order_was_placed()
    {
        var rental = PlacedRental();
        rental.MarkPaid(Placed);

        var action = () => rental.ScheduleDelivery(Placed.AddDays(-1));

        action.Should().Throw<DomainRuleViolationException>();
    }
}
