using CoreRentalNet.BuildingBlocks.Domain;

namespace CoreRentalNet.Modules.Rentals.Domain;

/// <summary>
/// A line of an order, with the price frozen at the moment it was placed. A later catalog
/// change can never rewrite what this order costs (ADR-0006).
/// </summary>
public sealed record RentalLine
{
    public RentalLine(string sku, string name, int quantity, Money unitMonthlyPrice)
    {
        if (quantity < 1)
        {
            throw new DomainRuleViolationException($"A line needs at least one unit, but {quantity} was given.");
        }

        Sku = Guard.NotEmpty(sku, "SKU", 32).ToUpperInvariant();
        Name = Guard.NotEmpty(name, "Line name", 120);
        Quantity = quantity;
        UnitMonthlyPrice = unitMonthlyPrice ?? throw new DomainRuleViolationException("A line requires a unit price.");

        // The single rounding point for this line.
        LineTotal = unitMonthlyPrice.Times(quantity).Round();
    }

    public string Sku { get; }

    public string Name { get; }

    public int Quantity { get; }

    public Money UnitMonthlyPrice { get; }

    public Money LineTotal { get; }
}
