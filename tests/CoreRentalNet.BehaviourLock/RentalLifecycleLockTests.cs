using AwesomeAssertions;
using CoreRentalNet.BuildingBlocks.Application;
using CoreRentalNet.BuildingBlocks.Domain;
using CoreRentalNet.Modules.Rentals.Application.Rentals;
using Xunit;
using Rental = CoreRentalNet.Modules.Rentals.Domain.Rentals.Rental;
using CoreRentalNet.Modules.Rentals.Domain.Rentals;

namespace CoreRentalNet.BehaviourLock;

/// <summary>
/// Stage-0 behaviour lock for the rental lifecycle and its refusals.
/// </summary>
/// <remarks>
/// Repointed, not relaxed, when the state machine moved from the aggregate to
/// <see cref="RentalLifecycleService"/> in stage 4: the invocation changed, the expected wording did
/// not. Messages that embed a formatted date match a wildcard for the date only. The three guards that
/// used to sit on <c>Rental.Place</c>/<c>RentalLine</c> are now asserted in <c>PlaceOrderTests</c>,
/// against the service that owns them.
/// </remarks>
public sealed class RentalLifecycleLockTests
{
    private static readonly DateOnly Placed = new(2026, 1, 31);

    private static readonly IRenewalPolicyService Renewals = new RenewalPolicyService();
    private static readonly IDeliveryPolicyService Deliveries = new DeliveryPolicyService();
    private static readonly ICancellationPolicyService Cancellations = new CancellationPolicyService(Renewals);
    private static readonly IRentalLifecycleService Lifecycle =
        new RentalLifecycleService(new MoneyService(), Renewals, Deliveries, Cancellations);

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

    private static Rental ActiveRental()
    {
        var rental = PlacedRental();
        Lifecycle.MarkPaid(rental, Placed);
        Lifecycle.ScheduleDelivery(rental, Deliveries.ScheduledFor(Placed));
        Lifecycle.Activate(rental, Deliveries.ScheduledFor(Placed));
        return rental;
    }

    [Fact]
    public void Activating_before_delivery_is_scheduled_is_refused()
    {
        var rental = PlacedRental();

        var act = () => Lifecycle.Activate(rental, Placed);

        act.Should().Throw<DomainRuleViolationException>()
            .WithMessage("Cannot activate: this order is Placed, not DeliveryScheduled.");
    }

    [Fact]
    public void Scheduling_delivery_before_payment_is_refused()
    {
        var rental = PlacedRental();

        var act = () => Lifecycle.ScheduleDelivery(rental, Placed);

        act.Should().Throw<DomainRuleViolationException>()
            .WithMessage("Cannot schedule delivery: this order is Placed, not Paid.");
    }

    [Fact]
    public void Ending_an_order_that_is_not_awaiting_its_end_is_refused()
    {
        var rental = PlacedRental();

        var act = () => Lifecycle.End(rental, Placed);

        act.Should().Throw<DomainRuleViolationException>()
            .WithMessage("Cannot end: this order is Placed, not CancellationRequested.");
    }

    [Fact]
    public void Paying_before_the_order_was_placed_is_refused()
    {
        var rental = PlacedRental();

        var act = () => Lifecycle.MarkPaid(rental, Placed.AddDays(-1));

        act.Should().Throw<DomainRuleViolationException>()
            .WithMessage("A payment date cannot be before the order date (*).");
    }

    [Fact]
    public void Activating_before_the_scheduled_delivery_date_is_refused()
    {
        var rental = PlacedRental();
        Lifecycle.MarkPaid(rental, Placed);
        Lifecycle.ScheduleDelivery(rental, Deliveries.ScheduledFor(Placed));

        var act = () => Lifecycle.Activate(rental, Placed);

        act.Should().Throw<DomainRuleViolationException>()
            .WithMessage("This order is scheduled for * and cannot activate on *.");
    }

    [Fact]
    public void Ending_before_the_paid_period_is_over_is_refused()
    {
        var rental = ActiveRental();
        Lifecycle.RequestCancellation(rental, new DateOnly(2026, 2, 5));

        var act = () => Lifecycle.End(rental, new DateOnly(2026, 2, 10));

        act.Should().Throw<DomainRuleViolationException>()
            .WithMessage("This order runs until * and cannot end on *.");
    }

    [Fact]
    public void Cancelling_outright_after_activation_is_refused()
    {
        var rental = ActiveRental();

        var act = () => Lifecycle.CancelBeforeDelivery(rental, Placed);

        act.Should().Throw<DomainRuleViolationException>()
            .WithMessage("A Active order cannot be cancelled outright; request cancellation instead.");
    }

    [Fact]
    public void Cancelling_an_ended_order_is_refused()
    {
        var rental = ActiveRental();
        Lifecycle.RequestCancellation(rental, new DateOnly(2026, 2, 5));
        Lifecycle.End(rental, new DateOnly(2026, 2, 28));

        var act = () => Lifecycle.RequestCancellation(rental, new DateOnly(2026, 3, 1));

        act.Should().Throw<DomainRuleViolationException>()
            .WithMessage("A Ended order cannot be cancelled.");
    }

    [Fact]
    public void Every_accepted_transition_bumps_the_concurrency_version()
    {
        var rental = PlacedRental();
        var version = rental.Version;

        Lifecycle.MarkPaid(rental, Placed);
        Lifecycle.ScheduleDelivery(rental, Deliveries.ScheduledFor(Placed));
        Lifecycle.Activate(rental, Deliveries.ScheduledFor(Placed));

        rental.Version.Should().Be(version + 3, "a write service must bump Version on every accepted transition");
    }
}
