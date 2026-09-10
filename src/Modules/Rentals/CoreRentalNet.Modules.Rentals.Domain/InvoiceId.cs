using CoreRentalNet.BuildingBlocks.Domain;

namespace CoreRentalNet.Modules.Rentals.Domain;

public readonly record struct InvoiceId(Guid Value)
{
    public static InvoiceId New() => new(Guid.NewGuid());

    public static InvoiceId From(Guid value)
        => value == Guid.Empty ? throw new DomainRuleViolationException("An invoice id cannot be empty.") : new InvoiceId(value);

    public override string ToString() => Value.ToString();
}
