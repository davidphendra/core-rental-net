using CoreRentalNet.BuildingBlocks.Domain;

namespace CoreRentalNet.Modules.Rentals.Domain.Rentals;

/// <summary>
/// A placed order, as it is stored: what was rented, at what price, where it goes, and how far
/// through its life it is.
/// </summary>
/// <remarks>
/// A plain persistence object. The status only ever moves forward and every move is a
/// named operation — but those operations live in <c>IRentalLifecycleService</c> now, not here.
/// </remarks>
public sealed class Rental
{
    public RentalId Id { get; set; }

    /// <summary>
    /// The draft this order came from. It is what makes checkout idempotent: the second attempt
    /// finds the order instead of placing another one.
    /// </summary>
    public Guid WorkspaceId { get; set; }

    public RentalNumber Number { get; set; } = null!;

    public string AccessTokenHash { get; set; } = string.Empty;

    public RentalStatus Status { get; set; }

    public string DeliveryAddress { get; set; } = string.Empty;

    public Money DeliveryFee { get; set; } = new Money(0m, Currencies.Idr);

    /// <summary>
    /// The date the monthly periods are measured from, stored once. Computing period N as
    /// <c>anchor.AddMonths(N)</c> is what stops the billing date drifting.
    /// </summary>
    public DateOnly AnchorDate { get; set; }

    public DateOnly PlacedOn { get; set; }

    public DateOnly? DeliveryScheduledFor { get; set; }

    public DateOnly? ActivatedOn { get; set; }

    public DateOnly? CancellationRequestedOn { get; set; }

    public DateOnly? EndsOn { get; set; }

    /// <summary>Bumped by every accepted transition and mapped as a concurrency token.</summary>
    public int Version { get; set; }

    public List<RentalLine> Lines { get; set; } = [];
}
