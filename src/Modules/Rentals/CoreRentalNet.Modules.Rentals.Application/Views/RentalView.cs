using CoreRentalNet.BuildingBlocks.Domain;
using CoreRentalNet.Modules.Rentals.Domain.Rentals;

namespace CoreRentalNet.Modules.Rentals.Application.Views;

/// <summary>
/// An order as the confirmation page needs it. Deliberately assembled from the frozen order, so
/// what is shown is what was charged.
/// </summary>
public sealed record RentalView(
    string Number,
    RentalStatus Status,
    string DeliveryAddress,
    DateOnly PlacedOn,
    DateOnly AnchorDate,
    Money MonthlyTotal,
    Money DeliveryFee,
    int TotalUnits,
    DateOnly? DeliveryScheduledFor,
    DateOnly? ActivatedOn,
    DateOnly? CancellationRequestedOn,
    DateOnly? EndsOn,
    DateOnly CurrentPeriodStart,
    DateOnly CurrentPeriodEnd,
    IReadOnlyList<RentalLineView> Lines);
