using CoreRentalNet.BuildingBlocks.Application;
using CoreRentalNet.BuildingBlocks.Domain;
using CoreRentalNet.Modules.Rentals.Domain.Rentals;

namespace CoreRentalNet.Modules.Rentals.Application.Rentals;

/// <summary>
/// Everything that used to live on the <c>Rental</c> aggregate, now in a service.
/// </summary>
/// <remarks>
/// The transitions and the refusal messages are unchanged: the behaviour lock in
/// <c>CoreRentalNet.BehaviourLock</c> pins them. What changed is where they are enforced.
/// </remarks>
public sealed class RentalLifecycleService(
    IMoneyService money,
    IRenewalPolicyService renewals,
    IDeliveryPolicyService deliveries,
    ICancellationPolicyService cancellations) : IRentalLifecycleService
{
    /// <summary>Summed from the frozen line prices, so it never changes after placement.</summary>
    public Money MonthlyTotal(Rental rental)
    {
        ArgumentNullException.ThrowIfNull(rental);

        return money.Round(money.Sum(rental.Lines.Select(LineTotal)));
    }

    public int TotalUnits(Rental rental)
    {
        ArgumentNullException.ThrowIfNull(rental);

        return rental.Lines.Sum(line => line.Quantity);
    }

    public RentalPeriod PeriodContaining(Rental rental, DateOnly date)
    {
        ArgumentNullException.ThrowIfNull(rental);

        return renewals.PeriodContaining(rental.AnchorDate, date);
    }

    public RentalPeriod NextPeriod(Rental rental, DateOnly asOf)
        => renewals.For(rental.AnchorDate, PeriodContaining(rental, asOf).Index + 1);

    public bool IsLive(Rental rental)
    {
        ArgumentNullException.ThrowIfNull(rental);

        return rental.Status is RentalStatus.Paid or RentalStatus.DeliveryScheduled or RentalStatus.Active;
    }

    public void MarkPaid(Rental rental, DateOnly on)
    {
        ArgumentNullException.ThrowIfNull(rental);
        EnsureStatus(rental, RentalStatus.Placed, "mark paid");
        EnsureNotBefore(on, rental.PlacedOn, "payment");
        rental.Status = RentalStatus.Paid;
        Touch(rental);
    }

    public void ScheduleDelivery(Rental rental, DateOnly forDate)
    {
        ArgumentNullException.ThrowIfNull(rental);
        EnsureStatus(rental, RentalStatus.Paid, "schedule delivery");
        deliveries.EnsureSchedulable(rental.PlacedOn, forDate);
        rental.DeliveryScheduledFor = forDate;
        rental.Status = RentalStatus.DeliveryScheduled;
        Touch(rental);
    }

    public void Activate(Rental rental, DateOnly on)
    {
        ArgumentNullException.ThrowIfNull(rental);
        EnsureStatus(rental, RentalStatus.DeliveryScheduled, "activate");

        if (rental.DeliveryScheduledFor is { } scheduled && on < scheduled)
        {
            throw new DomainRuleViolationException($"This order is scheduled for {scheduled} and cannot activate on {on}.");
        }

        rental.ActivatedOn = on;
        rental.Status = RentalStatus.Active;
        Touch(rental);
    }

    public void RequestCancellation(Rental rental, DateOnly on)
    {
        ArgumentNullException.ThrowIfNull(rental);

        if (rental.Status is not (RentalStatus.Paid or RentalStatus.DeliveryScheduled or RentalStatus.Active))
        {
            throw new DomainRuleViolationException($"A {rental.Status} order cannot be cancelled.");
        }

        EnsureNotBefore(on, rental.PlacedOn, "cancellation request");
        rental.CancellationRequestedOn = on;
        rental.EndsOn = cancellations.EffectiveEnd(rental.AnchorDate, on);
        rental.Status = RentalStatus.CancellationRequested;
        Touch(rental);
    }

    public void End(Rental rental, DateOnly on)
    {
        ArgumentNullException.ThrowIfNull(rental);
        EnsureStatus(rental, RentalStatus.CancellationRequested, "end");

        if (rental.EndsOn is { } endsOn && on < endsOn)
        {
            throw new DomainRuleViolationException($"This order runs until {endsOn} and cannot end on {on}.");
        }

        rental.Status = RentalStatus.Ended;
        Touch(rental);
    }

    public void CancelBeforeDelivery(Rental rental, DateOnly on)
    {
        ArgumentNullException.ThrowIfNull(rental);

        if (rental.Status is not (RentalStatus.Placed or RentalStatus.Paid or RentalStatus.DeliveryScheduled))
        {
            throw new DomainRuleViolationException($"A {rental.Status} order cannot be cancelled outright; request cancellation instead.");
        }

        EnsureNotBefore(on, rental.PlacedOn, "cancellation");
        rental.Status = RentalStatus.Cancelled;
        Touch(rental);
    }

    /// <summary>The single rounding point for a line: unit price times quantity, rounded once.</summary>
    private Money LineTotal(RentalLine line) => money.Round(money.Times(line.UnitMonthlyPrice, line.Quantity));

    private static void EnsureStatus(Rental rental, RentalStatus expected, string operation)
    {
        if (rental.Status != expected)
        {
            throw new DomainRuleViolationException($"Cannot {operation}: this order is {rental.Status}, not {expected}.");
        }
    }

    private static void EnsureNotBefore(DateOnly value, DateOnly earliest, string what)
    {
        if (value < earliest)
        {
            throw new DomainRuleViolationException($"A {what} date cannot be before the order date ({earliest}).");
        }
    }

    private static void Touch(Rental rental) => rental.Version++;
}
