using CoreRentalNet.BuildingBlocks.Domain;

namespace CoreRentalNet.Modules.Rentals.Domain.Rentals;

/// <summary>An order's identity. An empty one is refused, so no order can be addressed by nothing.</summary>
public readonly record struct RentalId(Guid Value)
{
    /// <summary>A new, non-empty identity.</summary>
    public static RentalId New() => new(Guid.NewGuid());

    /// <summary>Wraps an existing identity.</summary>
    /// <exception cref="DomainRuleViolationException">The value is <see cref="Guid.Empty"/>.</exception>
    public static RentalId From(Guid value)
        => value == Guid.Empty ? throw new DomainRuleViolationException("A rental id cannot be empty.") : new RentalId(value);

    public override string ToString() => Value.ToString();
}
