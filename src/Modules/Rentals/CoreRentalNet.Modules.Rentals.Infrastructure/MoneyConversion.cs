using CoreRentalNet.BuildingBlocks.Domain;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace CoreRentalNet.Modules.Rentals.Infrastructure;

/// <summary>
/// Stores a <see cref="Money"/> as a single decimal column.
/// </summary>
/// <remarks>
/// This application bills in rupiah only, so one column is honest rather than a
/// currency column that always says the same thing. The conversion goes through the domain's own
/// factory, so an amount cannot re-enter the domain unvalidated, and writing anything other than
/// IDR fails loudly instead of quietly losing the currency.
/// </remarks>
internal static class MoneyConversion
{
    public static ValueConverter<Money, decimal> Converter { get; } = new(
        money => ToAmount(money),
        amount => new Money(amount, Currencies.Idr));

    private static decimal ToAmount(Money money)
    {
        if (money is null)
        {
            throw new DomainRuleViolationException("A money value is required.");
        }

        if (money.Currency != Currencies.Idr)
        {
            throw new DomainRuleViolationException($"Only {Currencies.Idr} can be stored, but {money.Currency} was given.");
        }

        return money.Amount;
    }
}
