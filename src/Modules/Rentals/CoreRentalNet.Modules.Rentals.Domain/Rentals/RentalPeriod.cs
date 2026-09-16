namespace CoreRentalNet.Modules.Rentals.Domain.Rentals;

/// <summary>
/// One billed month, measured from the anchor the order was placed on.
/// </summary>
/// <remarks>
/// The start is always <c>anchor.AddMonths(index)</c>, never the previous period's end. That is
/// what makes an order placed on the 31st clamp to the 28th in February and then return to the
/// 31st in March, with no special case. The arithmetic that produces a period lives in
/// <c>IRenewalPolicyService</c>.
/// </remarks>
public readonly record struct RentalPeriod(int Index, DateOnly Start, DateOnly EndExclusive)
{
    /// <summary>The last day the customer has the equipment, inclusive.</summary>
    public DateOnly LastDay => EndExclusive.AddDays(-1);

    public bool Contains(DateOnly date) => date >= Start && date < EndExclusive;
}
