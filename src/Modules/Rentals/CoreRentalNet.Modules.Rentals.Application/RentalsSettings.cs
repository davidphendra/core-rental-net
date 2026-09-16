using CoreRentalNet.BuildingBlocks.Domain;

namespace CoreRentalNet.Modules.Rentals.Application;

/// <summary>
/// What this module charges beyond the catalog: the one-time delivery and setup charge, and the
/// tax rate.
/// </summary>
/// <remarks>
/// The tax rate is seeded to zero so the designed totals read exactly as drawn; the pipeline is
/// in place, so enabling it is configuration rather than a schema change.
/// </remarks>
public sealed record RentalsSettings
{
    public RentalsSettings(Money deliveryFee, decimal taxRate = 0m)
    {
        if (taxRate is < 0m or > 1m)
        {
            throw new DomainRuleViolationException($"A tax rate is a fraction between 0 and 1, but {taxRate} was given.");
        }

        DeliveryFee = deliveryFee ?? throw new DomainRuleViolationException("A delivery fee is required.");
        TaxRate = taxRate;
    }

    public Money DeliveryFee { get; }

    public decimal TaxRate { get; }
}
