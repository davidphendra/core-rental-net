using CoreRentalNet.BuildingBlocks.Domain;

namespace CoreRentalNet.Modules.Rentals.Domain.Invoices;

/// <summary>An invoice's identity. An empty one is refused, so no invoice can be addressed by nothing.</summary>
public readonly record struct InvoiceId(Guid Value)
{
    /// <summary>A new, non-empty identity.</summary>
    public static InvoiceId New() => new(Guid.NewGuid());

    /// <summary>Wraps an existing identity.</summary>
    /// <exception cref="DomainRuleViolationException">The value is <see cref="Guid.Empty"/>.</exception>
    public static InvoiceId From(Guid value)
        => value == Guid.Empty ? throw new DomainRuleViolationException("An invoice id cannot be empty.") : new InvoiceId(value);

    public override string ToString() => Value.ToString();
}
