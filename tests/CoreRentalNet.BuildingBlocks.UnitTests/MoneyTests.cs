using AwesomeAssertions;
using CoreRentalNet.BuildingBlocks.Application;
using CoreRentalNet.BuildingBlocks.Domain;
using Xunit;

namespace CoreRentalNet.BuildingBlocks.UnitTests;

/// <summary>
/// The money rules, now enforced by <see cref="MoneyService"/> against a plain record.
/// Operators and <c>CompareTo</c> were removed with the behaviour; nothing in the application sorted
/// or compared money with them.
/// </summary>
public sealed class MoneyTests
{
    private static readonly IMoneyService Service = new MoneyService();

    private static Money Idr(decimal amount) => new(amount, Currencies.Idr);

    [Fact] // MON-01
    public void Adding_two_amounts_of_the_same_currency_sums_them()
    {
        var sum = Service.Add(Idr(400000m), Idr(250000m));

        sum.Amount.Should().Be(650000m);
        sum.Currency.Should().Be(Currencies.Idr);
    }

    [Fact] // MON-02
    public void Combining_two_different_currencies_is_rejected()
    {
        var action = () => Service.Add(Idr(400000m), new Money(10m, "USD"));

        action.Should().Throw<DomainRuleViolationException>().WithMessage("*IDR*USD*");
    }

    [Fact] // MON-03
    public void Multiplying_by_a_quantity_scales_the_amount()
    {
        var total = Service.Times(Idr(400000m), 3);

        total.Amount.Should().Be(1200000m);
    }

    [Theory] // MON-04
    [InlineData(400000.555, 400000.56)]
    [InlineData(400000.554, 400000.55)]
    [InlineData(0.125, 0.13)]
    [InlineData(0.135, 0.14)]
    public void Rounding_goes_away_from_zero_at_the_midpoint(decimal input, decimal expected)
    {
        Service.Round(Idr(input)).Amount.Should().Be(expected);
    }

    [Fact] // MON-05
    public void Rounding_happens_once_at_the_end_and_is_observably_different_from_rounding_each_line()
    {
        Money[] lines = [Idr(100000.005m), Idr(100000.005m), Idr(100000.005m)];

        var roundedOnce = Service.Round(Service.Sum(lines));

        var roundedPerLine = Service.Sum(lines.Select(Service.Round));

        roundedOnce.Amount.Should().Be(300000.02m);
        roundedPerLine.Amount.Should().Be(300000.03m);
        roundedOnce.Amount.Should().NotBe(roundedPerLine.Amount, "the domain policy is to round once, at the end");
    }

    [Fact] // MON-06
    public void The_currency_format_is_the_cultures_and_reads_in_rupiah()
    {
        // The prefix and the group separator belong to the configured culture; the zero decimals
        // belong to the currency, because ICU reports two for en-ID on Linux and zero on macOS.
        // The Host sets CurrentCulture at start-up from the name appsettings.json ships, through
        // the same helper this names. The old "1,234.50 USD" case went with the hard-coded
        // formatter, because no production path builds a non-rupiah amount.
        var business = BusinessCulture.For("en-ID");

        Idr(400000m).Amount.ToString("C", business).Should().Be("Rp400.000");
        Idr(1500000m).Amount.ToString("C", business).Should().Be("Rp1.500.000");
        Idr(0m).Amount.ToString("C", business).Should().Be("Rp0");
    }

    [Fact] // MON-07
    public void Zero_is_allowed_but_negative_amounts_are_not()
    {
        Idr(0m).Amount.Should().Be(0m);

        var negative = () => Service.Idr(-1m);
        negative.Should().Throw<DomainRuleViolationException>().WithMessage("*negative*");
    }

    [Fact] // MON-07
    public void Subtracting_more_than_the_amount_is_rejected()
    {
        var action = () => Service.Subtract(Idr(100m), Idr(101m));

        action.Should().Throw<DomainRuleViolationException>().WithMessage("*negative*");
    }

    [Fact] // MON-07
    public void Multiplying_by_a_negative_quantity_is_rejected()
    {
        var action = () => Service.Times(Idr(100m), -1);

        action.Should().Throw<DomainRuleViolationException>();
    }

    [Fact] // MON-07
    public void Summing_nothing_is_rejected_rather_than_returning_a_currencyless_zero()
    {
        var action = () => Service.Sum([]);

        action.Should().Throw<DomainRuleViolationException>();
    }

    [Fact]
    public void Equality_includes_the_currency()
    {
        Idr(100m).Should().Be(Idr(100m));
        Idr(100m).Should().NotBe(new Money(100m, "USD"));
    }
}
