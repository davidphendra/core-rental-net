using CoreRentalNet.BuildingBlocks.Domain;
using CoreRentalNet.Modules.Rentals.Domain.Rentals;

namespace CoreRentalNet.Modules.Rentals.Application.Services;

/// <summary>
/// The rental's state machine and its plain projections, moved off the <c>Rental</c> aggregate
///.
/// </summary>
/// <remarks>
/// Every transition is a named operation, so an illegal one is an error rather than a state nobody
/// thought about. Every accepted transition bumps <see cref="Rental.Version"/>, which is now this
/// service's job rather than the record's.
/// </remarks>
public interface IRentalLifecycleService
{
    /// <summary>Summed from the frozen line prices, so it never changes after placement.</summary>
    Money MonthlyTotal(Rental rental);

    int TotalUnits(Rental rental);

    /// <summary>The period a date falls in, measured from the anchor.</summary>
    RentalPeriod PeriodContaining(Rental rental, DateOnly date);

    /// <summary>The period that renews next, given a date inside the current one.</summary>
    RentalPeriod NextPeriod(Rental rental, DateOnly asOf);

    bool IsLive(Rental rental);

    void MarkPaid(Rental rental, DateOnly on);

    void ScheduleDelivery(Rental rental, DateOnly forDate);

    void Activate(Rental rental, DateOnly on);

    /// <summary>Takes effect at the end of the period already paid for; the next renewal is suppressed.</summary>
    void RequestCancellation(Rental rental, DateOnly on);

    void End(Rental rental, DateOnly on);

    /// <summary>Cancels before anything was delivered, which is the only case with nothing to collect.</summary>
    void CancelBeforeDelivery(Rental rental, DateOnly on);
}
