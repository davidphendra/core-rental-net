using CoreRentalNet.BuildingBlocks.Domain;
using CoreRentalNet.Modules.Rentals.Domain.Rentals;

namespace CoreRentalNet.Modules.Rentals.Application.Rules;

/// <summary>
/// Anchor-based period arithmetic, moved off the Domain's static policy.
/// </summary>
/// <remarks>
/// The start of period N is always <c>anchor.AddMonths(N)</c>, never the previous period's end. That
/// is what makes an order placed on the 31st clamp to the 28th in February and then return to the
/// 31st in March, with no special case.
/// </remarks>
public sealed class RenewalPolicyService : IRenewalPolicyService
{
    /// <summary>A guard against a runaway loop rather than a business limit: fifty years.</summary>
    public const int MaxPeriodsValue = 600;

    public int MaxPeriods => MaxPeriodsValue;

    public int PeriodIndexContaining(DateOnly anchor, DateOnly date)
    {
        if (date < anchor)
        {
            throw new DomainRuleViolationException($"Date {date} is before the anchor {anchor}.");
        }

        var index = 0;

        while (index < MaxPeriodsValue && anchor.AddMonths(index + 1) <= date)
        {
            index++;
        }

        if (index >= MaxPeriodsValue)
        {
            throw new DomainRuleViolationException($"Date {date} is too far past the anchor {anchor}.");
        }

        return index;
    }

    public RentalPeriod PeriodContaining(DateOnly anchor, DateOnly date)
        => For(anchor, PeriodIndexContaining(anchor, date));

    public RentalPeriod For(DateOnly anchor, int index)
    {
        if (index < 0)
        {
            throw new DomainRuleViolationException($"A period index cannot be negative, but {index} was given.");
        }

        return new RentalPeriod(index, anchor.AddMonths(index), anchor.AddMonths(index + 1));
    }
}
