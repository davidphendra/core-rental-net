using CoreRentalNet.BuildingBlocks.Domain;
using CoreRentalNet.Modules.Rentals.Domain.Rentals;

namespace CoreRentalNet.Modules.Rentals.Application.Rules;

/// <summary>Where a given date falls in a rental that bills monthly from an anchor.</summary>
public interface IRenewalPolicyService
{
    /// <summary>A guard against a runaway loop rather than a business limit: fifty years.</summary>
    int MaxPeriods { get; }

    int PeriodIndexContaining(DateOnly anchor, DateOnly date);

    RentalPeriod PeriodContaining(DateOnly anchor, DateOnly date);

    /// <summary>The period at an index, measured from the anchor. Clamping lives in <c>AddMonths</c>.</summary>
    RentalPeriod For(DateOnly anchor, int index);
}
