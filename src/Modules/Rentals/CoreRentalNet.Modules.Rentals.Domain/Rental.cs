using CoreRentalNet.BuildingBlocks.Domain;

namespace CoreRentalNet.Modules.Rentals.Domain;

/// <summary>
/// A placed order: what was rented, at what price, where it goes, and how far through its life
/// it is. The status only ever moves forward, and every move is a named operation so an illegal
/// transition is an error rather than a state nobody thought about.
/// </summary>
public sealed class Rental
{
    private readonly List<RentalLine> lines = [];

    private Rental()
    {
        Number = null!;
        AccessTokenHash = string.Empty;
        DeliveryAddress = string.Empty;
        DeliveryFee = Money.Idr(0m);
    }

    private Rental(
        RentalId id,
        Guid workspaceId,
        RentalNumber number,
        string accessTokenHash,
        string deliveryAddress,
        Money deliveryFee,
        IReadOnlyList<RentalLine> lines,
        DateOnly placedOn)
    {
        if (lines.Count == 0)
        {
            throw new DomainRuleViolationException("An order needs at least one line.");
        }

        Id = RentalId.From(id.Value);
        WorkspaceId = workspaceId == Guid.Empty
            ? throw new DomainRuleViolationException("An order must remember the workspace it came from.")
            : workspaceId;
        Number = number ?? throw new DomainRuleViolationException("An order requires a number.");
        AccessTokenHash = Guard.NotEmpty(accessTokenHash, "Access token hash", 64);
        DeliveryAddress = Guard.NotEmpty(deliveryAddress, "Delivery address", 200);
        DeliveryFee = deliveryFee ?? throw new DomainRuleViolationException("An order requires a delivery fee.");
        PlacedOn = placedOn;
        AnchorDate = placedOn;
        Status = RentalStatus.Placed;
        Version = 1;
        this.lines.AddRange(lines);
    }

    public RentalId Id { get; private set; }

    /// <summary>
    /// The draft this order came from. It is what makes checkout idempotent: the second attempt
    /// finds the order instead of placing another one.
    /// </summary>
    public Guid WorkspaceId { get; private set; }

    public RentalNumber Number { get; private set; }

    public string AccessTokenHash { get; private set; }

    public RentalStatus Status { get; private set; }

    public string DeliveryAddress { get; private set; }

    public Money DeliveryFee { get; private set; }

    /// <summary>
    /// The date the monthly periods are measured from, stored once. Computing period N as
    /// <c>anchor.AddMonths(N)</c> is what stops the billing date drifting.
    /// </summary>
    public DateOnly AnchorDate { get; private set; }

    public DateOnly PlacedOn { get; private set; }

    public DateOnly? DeliveryScheduledFor { get; private set; }

    public DateOnly? ActivatedOn { get; private set; }

    public DateOnly? CancellationRequestedOn { get; private set; }

    public DateOnly? EndsOn { get; private set; }

    public int Version { get; private set; }

    public IReadOnlyList<RentalLine> Lines => lines;

    /// <summary>Summed from the frozen line prices, so it never changes after placement.</summary>
    public Money MonthlyTotal => Money.Sum(lines.Select(line => line.LineTotal)).Round();

    public int TotalUnits => lines.Sum(line => line.Quantity);

    public bool IsLive => Status is RentalStatus.Paid or RentalStatus.DeliveryScheduled or RentalStatus.Active;

    public static Rental Place(
        RentalId id,
        Guid workspaceId,
        RentalNumber number,
        string accessTokenHash,
        string deliveryAddress,
        Money deliveryFee,
        IReadOnlyList<RentalLine> lines,
        DateOnly placedOn)
        => new(id, workspaceId, number, accessTokenHash, deliveryAddress, deliveryFee, lines, placedOn);

    /// <summary>The period a date falls in, measured from the anchor.</summary>
    public RentalPeriod PeriodContaining(DateOnly date) => RenewalPolicy.PeriodContaining(AnchorDate, date);

    /// <summary>The period that renews next, given a date inside the current one.</summary>
    public RentalPeriod NextPeriod(DateOnly asOf) => RentalPeriod.For(AnchorDate, PeriodContaining(asOf).Index + 1);

    public void MarkPaid(DateOnly on)
    {
        EnsureStatus(RentalStatus.Placed, "mark paid");
        EnsureNotBefore(on, PlacedOn, "payment");
        Status = RentalStatus.Paid;
        Touch();
    }

    public void ScheduleDelivery(DateOnly forDate)
    {
        EnsureStatus(RentalStatus.Paid, "schedule delivery");
        DeliveryPolicy.EnsureSchedulable(PlacedOn, forDate);
        DeliveryScheduledFor = forDate;
        Status = RentalStatus.DeliveryScheduled;
        Touch();
    }

    public void Activate(DateOnly on)
    {
        EnsureStatus(RentalStatus.DeliveryScheduled, "activate");

        if (DeliveryScheduledFor is { } scheduled && on < scheduled)
        {
            throw new DomainRuleViolationException($"This order is scheduled for {scheduled} and cannot activate on {on}.");
        }

        ActivatedOn = on;
        Status = RentalStatus.Active;
        Touch();
    }

    /// <summary>
    /// Requests cancellation. It takes effect at the end of the period already paid for, and the
    /// next renewal is suppressed.
    /// </summary>
    public void RequestCancellation(DateOnly on)
    {
        if (Status is not (RentalStatus.Paid or RentalStatus.DeliveryScheduled or RentalStatus.Active))
        {
            throw new DomainRuleViolationException($"A {Status} order cannot be cancelled.");
        }

        EnsureNotBefore(on, PlacedOn, "cancellation request");
        CancellationRequestedOn = on;
        EndsOn = CancellationPolicy.EffectiveEnd(AnchorDate, on);
        Status = RentalStatus.CancellationRequested;
        Touch();
    }

    public void End(DateOnly on)
    {
        EnsureStatus(RentalStatus.CancellationRequested, "end");

        if (EndsOn is { } endsOn && on < endsOn)
        {
            throw new DomainRuleViolationException($"This order runs until {endsOn} and cannot end on {on}.");
        }

        Status = RentalStatus.Ended;
        Touch();
    }

    /// <summary>Cancels before anything was delivered, which is the only case with nothing to collect.</summary>
    public void CancelBeforeDelivery(DateOnly on)
    {
        if (Status is not (RentalStatus.Placed or RentalStatus.Paid or RentalStatus.DeliveryScheduled))
        {
            throw new DomainRuleViolationException($"A {Status} order cannot be cancelled outright; request cancellation instead.");
        }

        EnsureNotBefore(on, PlacedOn, "cancellation");
        Status = RentalStatus.Cancelled;
        Touch();
    }

    private void EnsureStatus(RentalStatus expected, string operation)
    {
        if (Status != expected)
        {
            throw new DomainRuleViolationException($"Cannot {operation}: this order is {Status}, not {expected}.");
        }
    }

    private static void EnsureNotBefore(DateOnly value, DateOnly earliest, string what)
    {
        if (value < earliest)
        {
            throw new DomainRuleViolationException($"A {what} date cannot be before the order date ({earliest}).");
        }
    }

    private void Touch() => Version++;
}
