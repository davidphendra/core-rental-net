using System.Globalization;

namespace CoreRentalNet.BuildingBlocks.Domain;

/// <summary>
/// A monetary amount in a single currency.
/// </summary>
/// <remarks>
/// Deliberately a reference type rather than a struct: a struct would allow
/// <c>default(Money)</c>, which would carry a null currency and break the invariant.
/// Amounts are never negative; zero is allowed. Rounding happens exactly once, at the
/// caller's initiative, via <see cref="Round"/>.
/// </remarks>
public sealed record Money : IComparable<Money>, IComparable
{
    private static readonly CultureInfo IdId = CultureInfo.GetCultureInfo("id-ID");

    private Money(decimal amount, string currency)
    {
        if (string.IsNullOrWhiteSpace(currency))
        {
            throw new DomainRuleViolationException("A money value requires a currency.");
        }

        if (amount < 0m)
        {
            throw new DomainRuleViolationException($"A money amount cannot be negative, but was {amount}.");
        }

        Amount = amount;
        Currency = currency;
    }

    public decimal Amount { get; }

    public string Currency { get; }

    public static Money Of(decimal amount, string currency) => new(amount, currency);

    public static Money Idr(decimal amount) => new(amount, Currencies.Idr);

    public static Money Zero(string currency) => new(0m, currency);

    public static Money Sum(IEnumerable<Money> items)
    {
        ArgumentNullException.ThrowIfNull(items);

        Money? total = null;

        foreach (var item in items)
        {
            total = total is null ? item : total.Add(item);
        }

        return total ?? throw new DomainRuleViolationException("Cannot sum an empty sequence of money values.");
    }

    /// <summary>Rounds to two decimals, away from zero. The single rounding point in the domain.</summary>
    public static decimal RoundAmount(decimal value) => Math.Round(value, 2, MidpointRounding.AwayFromZero);

    public Money Add(Money other)
    {
        EnsureSameCurrency(other);
        return new Money(Amount + other.Amount, Currency);
    }

    public Money Subtract(Money other)
    {
        EnsureSameCurrency(other);

        if (other.Amount > Amount)
        {
            throw new DomainRuleViolationException(
                $"Cannot subtract {other.Amount} {other.Currency} from {Amount} {Currency}: the result would be negative.");
        }

        return new Money(Amount - other.Amount, Currency);
    }

    public Money Times(int quantity)
    {
        if (quantity < 0)
        {
            throw new DomainRuleViolationException($"Cannot multiply money by a negative quantity ({quantity}).");
        }

        return new Money(Amount * quantity, Currency);
    }

    public Money Round() => new(RoundAmount(Amount), Currency);

    /// <summary>Renders the amount for display, e.g. <c>Rp400.000</c> for IDR.</summary>
    public string ToDisplayString() => Currency == Currencies.Idr
        ? "Rp" + Amount.ToString("N0", IdId)
        : Amount.ToString("N2", CultureInfo.InvariantCulture) + " " + Currency;

    public int CompareTo(Money? other)
    {
        if (other is null)
        {
            return 1;
        }

        EnsureSameCurrency(other);
        return Amount.CompareTo(other.Amount);
    }

    public int CompareTo(object? obj)
        => obj is null ? 1
            : obj is not Money money ? throw new ArgumentException($"Cannot compare Money with {obj.GetType().Name}.", nameof(obj))
            : CompareTo(money);

    public static bool operator <(Money left, Money right) => left.CompareTo(right) < 0;

    public static bool operator >(Money left, Money right) => left.CompareTo(right) > 0;

    public static bool operator <=(Money left, Money right) => left.CompareTo(right) <= 0;

    public static bool operator >=(Money left, Money right) => left.CompareTo(right) >= 0;

    public static Money operator +(Money left, Money right) => left.Add(right);

    public static Money operator -(Money left, Money right) => left.Subtract(right);

    public override string ToString() => ToDisplayString();

    private void EnsureSameCurrency(Money other)
    {
        if (!string.Equals(Currency, other.Currency, StringComparison.Ordinal))
        {
            throw new DomainRuleViolationException($"Cannot combine {Currency} with {other.Currency}.");
        }
    }
}
