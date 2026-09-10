using CoreRentalNet.BuildingBlocks.Domain;

namespace CoreRentalNet.Modules.Rentals.Domain;

/// <summary>
/// One billed month, measured from the anchor the order was placed on.
/// </summary>
/// <remarks>
/// The start is always <c>anchor.AddMonths(index)</c>, never the previous period's end. That is
/// what makes an order placed on the 31st clamp to the 28th in February and then return to the
/// 31st in March, with no special case (ADR-0011).
/// </remarks>
public readonly record struct RentalPeriod(int Index, DateOnly Start, DateOnly EndExclusive)
{
    public static RentalPeriod For(DateOnly anchor, int index)
    {
        if (index < 0)
        {
            throw new DomainRuleViolationException($"A period index cannot be negative, but {index} was given.");
        }

        return new RentalPeriod(index, anchor.AddMonths(index), anchor.AddMonths(index + 1));
    }

    /// <summary>The last day the customer has the equipment, inclusive.</summary>
    public DateOnly LastDay => EndExclusive.AddDays(-1);

    public bool Contains(DateOnly date) => date >= Start && date < EndExclusive;
}
