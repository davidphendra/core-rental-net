using CoreRentalNet.BuildingBlocks.Domain;

namespace CoreRentalNet.BuildingBlocks.Application;

/// <summary>Every rule about money, moved off the <c>Money</c> value object.</summary>
public sealed class MoneyService : IMoneyService
{
    /// <inheritdoc />
    public Money Idr(decimal amount)
    {
        EnsureNonNegative(amount);

        return new Money(amount, Currencies.Idr);
    }

    /// <inheritdoc />
    public Money Add(Money left, Money right)
    {
        ArgumentNullException.ThrowIfNull(left);
        ArgumentNullException.ThrowIfNull(right);
        EnsureSameCurrency(left, right);

        var amount = left.Amount + right.Amount;
        EnsureNonNegative(amount);

        return new Money(amount, left.Currency);
    }

    /// <inheritdoc />
    public Money Subtract(Money left, Money right)
    {
        ArgumentNullException.ThrowIfNull(left);
        ArgumentNullException.ThrowIfNull(right);
        EnsureSameCurrency(left, right);

        if (right.Amount > left.Amount)
        {
            throw new DomainRuleViolationException(
                $"Cannot subtract {right.Amount} {right.Currency} from {left.Amount} {left.Currency}: the result would be negative.");
        }

        return new Money(left.Amount - right.Amount, left.Currency);
    }

    /// <inheritdoc />
    public Money Times(Money money, int quantity)
    {
        ArgumentNullException.ThrowIfNull(money);

        if (quantity < 0)
        {
            throw new DomainRuleViolationException($"Cannot multiply money by a negative quantity ({quantity}).");
        }

        return new Money(money.Amount * quantity, money.Currency);
    }

    /// <inheritdoc />
    public Money Round(Money money)
    {
        ArgumentNullException.ThrowIfNull(money);

        return new Money(RoundAmount(money.Amount), money.Currency);
    }

    /// <inheritdoc />
    public Money Sum(IEnumerable<Money> items)
    {
        ArgumentNullException.ThrowIfNull(items);

        Money? total = null;

        foreach (var item in items)
        {
            total = total is null ? item : Add(total, item);
        }

        return total ?? throw new DomainRuleViolationException("Cannot sum an empty sequence of money values.");
    }

    /// <inheritdoc />
    public decimal RoundAmount(decimal value) => Math.Round(value, 2, MidpointRounding.AwayFromZero);

    /// <inheritdoc />
    public void EnsureSameCurrency(Money left, Money right)
    {
        ArgumentNullException.ThrowIfNull(left);
        ArgumentNullException.ThrowIfNull(right);

        if (!string.Equals(left.Currency, right.Currency, StringComparison.Ordinal))
        {
            throw new DomainRuleViolationException($"Cannot combine {left.Currency} with {right.Currency}.");
        }
    }

    private static void EnsureNonNegative(decimal amount)
    {
        if (amount < 0m)
        {
            throw new DomainRuleViolationException($"A money amount cannot be negative, but was {amount}.");
        }
    }
}
