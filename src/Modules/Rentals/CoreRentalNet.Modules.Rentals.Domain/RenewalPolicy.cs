using CoreRentalNet.BuildingBlocks.Domain;

namespace CoreRentalNet.Modules.Rentals.Domain;

/// <summary>Where a given date falls in a rental that bills monthly from an anchor.</summary>
public static class RenewalPolicy
{
    /// <summary>A guard against a runaway loop rather than a business limit: fifty years.</summary>
    public const int MaxPeriods = 600;

    public static int PeriodIndexContaining(DateOnly anchor, DateOnly date)
    {
        if (date < anchor)
        {
            throw new DomainRuleViolationException($"Date {date} is before the anchor {anchor}.");
        }

        var index = 0;

        while (index < MaxPeriods && anchor.AddMonths(index + 1) <= date)
        {
            index++;
        }

        if (index >= MaxPeriods)
        {
            throw new DomainRuleViolationException($"Date {date} is too far past the anchor {anchor}.");
        }

        return index;
    }

    public static RentalPeriod PeriodContaining(DateOnly anchor, DateOnly date)
        => RentalPeriod.For(anchor, PeriodIndexContaining(anchor, date));
}
