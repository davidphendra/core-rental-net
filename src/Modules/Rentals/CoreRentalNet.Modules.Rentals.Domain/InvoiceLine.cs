using CoreRentalNet.BuildingBlocks.Domain;

namespace CoreRentalNet.Modules.Rentals.Domain;

/// <summary>A frozen copy of an order line as it stood when the invoice was issued.</summary>
public sealed record InvoiceLine
{
    public InvoiceLine(string sku, string name, int quantity, Money unitMonthlyPrice, Money lineTotal)
    {
        Sku = Guard.NotEmpty(sku, "SKU", 32).ToUpperInvariant();
        Name = Guard.NotEmpty(name, "Line name", 120);
        Quantity = quantity < 1 ? throw new DomainRuleViolationException($"A line needs at least one unit, but {quantity} was given.") : quantity;
        UnitMonthlyPrice = unitMonthlyPrice ?? throw new DomainRuleViolationException("A line requires a unit price.");
        LineTotal = lineTotal ?? throw new DomainRuleViolationException("A line requires a total.");
    }

    public string Sku { get; }

    public string Name { get; }

    public int Quantity { get; }

    public Money UnitMonthlyPrice { get; }

    public Money LineTotal { get; }
}
