using CoreRentalNet.BuildingBlocks.Domain;

namespace CoreRentalNet.BuildingBlocks.Application;

/// <summary>
/// Every rule about money, moved off the <c>Money</c> value object.
/// </summary>
/// <remarks>
/// Amounts are never negative; zero is allowed. Rounding happens exactly once, at the caller's
/// initiative, via <see cref="Round"/>. Only IDR is ever combined with IDR.
/// </remarks>
public interface IMoneyService
{
    /// <summary>An IDR amount, refusing a negative one.</summary>
    /// <exception cref="DomainRuleViolationException">The amount is negative.</exception>
    Money Idr(decimal amount);

    /// <summary>Adds two amounts. The sum of two non-negative amounts is still non-negative.</summary>
    /// <exception cref="DomainRuleViolationException">The two amounts are in different currencies.</exception>
    Money Add(Money left, Money right);

    /// <summary>Subtracts <paramref name="right"/> from <paramref name="left"/>.</summary>
    /// <exception cref="DomainRuleViolationException">
    /// The two amounts are in different currencies, or the result would be negative.
    /// </exception>
    Money Subtract(Money left, Money right);

    /// <summary>Multiplies an amount by a whole quantity.</summary>
    /// <exception cref="DomainRuleViolationException">The quantity is negative.</exception>
    Money Times(Money money, int quantity);

    /// <summary>Rounds to two decimals, away from zero. The single rounding point.</summary>
    Money Round(Money money);

    /// <summary>Adds a sequence of amounts, which must share one currency.</summary>
    /// <exception cref="DomainRuleViolationException">
    /// The sequence is empty, so no currency could be carried, or its amounts disagree on one.
    /// </exception>
    Money Sum(IEnumerable<Money> items);

    /// <summary>Rounds a bare decimal to two decimals, away from zero. No currency is involved.</summary>
    decimal RoundAmount(decimal value);

    /// <summary>Refuses to combine two different currencies.</summary>
    /// <exception cref="DomainRuleViolationException">The two amounts are in different currencies.</exception>
    void EnsureSameCurrency(Money left, Money right);
}
