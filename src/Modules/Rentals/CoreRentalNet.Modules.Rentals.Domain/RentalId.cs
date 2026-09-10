using CoreRentalNet.BuildingBlocks.Domain;

namespace CoreRentalNet.Modules.Rentals.Domain;

public readonly record struct RentalId(Guid Value)
{
    public static RentalId New() => new(Guid.NewGuid());

    public static RentalId From(Guid value)
        => value == Guid.Empty ? throw new DomainRuleViolationException("A rental id cannot be empty.") : new RentalId(value);

    public override string ToString() => Value.ToString();
}
