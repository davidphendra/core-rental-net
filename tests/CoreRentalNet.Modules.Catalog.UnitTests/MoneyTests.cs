using AwesomeAssertions;
using CoreRentalNet.BuildingBlocks.Domain;
using Xunit;

namespace CoreRentalNet.Modules.Catalog.UnitTests;

public sealed class MoneyTests
{
    [Fact] // MON-01
    public void Adding_two_amounts_of_the_same_currency_sums_them()
    {
        var sum = Money.Idr(400000m).Add(Money.Idr(250000m));

        sum.Amount.Should().Be(650000m);
        sum.Currency.Should().Be(Currencies.Idr);
    }

    [Fact] // MON-02
    public void Combining_two_different_currencies_is_rejected()
    {
        var action = () => Money.Idr(400000m).Add(Money.Of(10m, "USD"));

        action.Should().Throw<DomainRuleViolationException>().WithMessage("*IDR*USD*");
    }

    [Fact] // MON-03
    public void Multiplying_by_a_quantity_scales_the_amount()
    {
        var total = Money.Idr(400000m).Times(3);

        total.Amount.Should().Be(1200000m);
    }

    [Theory] // MON-04
    [InlineData(400000.555, 400000.56)]
    [InlineData(400000.554, 400000.55)]
    [InlineData(0.125, 0.13)]
    [InlineData(0.135, 0.14)]
    public void Rounding_goes_away_from_zero_at_the_midpoint(decimal input, decimal expected)
    {
        Money.Idr(input).Round().Amount.Should().Be(expected);
    }

    [Fact] // MON-05
    public void Rounding_happens_once_at_the_end_and_is_observably_different_from_rounding_each_line()
    {
        Money[] lines = [Money.Idr(100000.005m), Money.Idr(100000.005m), Money.Idr(100000.005m)];

        var roundedOnce = Money.Sum(lines).Round();

        var roundedPerLine = Money.Sum(lines.Select(line => line.Round()));

        roundedOnce.Amount.Should().Be(300000.02m);
        roundedPerLine.Amount.Should().Be(300000.03m);
        roundedOnce.Amount.Should().NotBe(roundedPerLine.Amount, "the domain policy is to round once, at the end");
    }

    [Fact] // MON-06
    public void Display_format_is_indonesian_for_rupiah()
    {
        Money.Idr(400000m).ToDisplayString().Should().Be("Rp400.000");
        Money.Idr(1500000m).ToDisplayString().Should().Be("Rp1.500.000");
        Money.Idr(0m).ToDisplayString().Should().Be("Rp0");
        Money.Of(1234.5m, "USD").ToDisplayString().Should().Be("1,234.50 USD");
    }

    [Fact] // MON-07
    public void Zero_is_allowed_but_negative_amounts_are_not()
    {
        Money.Idr(0m).Amount.Should().Be(0m);
        Money.Zero(Currencies.Idr).Amount.Should().Be(0m);

        var negative = () => Money.Idr(-1m);
        negative.Should().Throw<DomainRuleViolationException>().WithMessage("*negative*");
    }

    [Fact] // MON-07
    public void Subtracting_more_than_the_amount_is_rejected()
    {
        var action = () => Money.Idr(100m).Subtract(Money.Idr(101m));

        action.Should().Throw<DomainRuleViolationException>().WithMessage("*negative*");
    }

    [Fact] // MON-07
    public void Multiplying_by_a_negative_quantity_is_rejected()
    {
        var action = () => Money.Idr(100m).Times(-1);

        action.Should().Throw<DomainRuleViolationException>();
    }

    [Fact] // MON-07
    public void Summing_nothing_is_rejected_rather_than_returning_a_currencyless_zero()
    {
        var action = () => Money.Sum([]);

        action.Should().Throw<DomainRuleViolationException>();
    }

    [Fact] // MON-01
    public void Operators_mirror_the_named_methods()
    {
        (Money.Idr(2m) + Money.Idr(3m)).Amount.Should().Be(5m);
        (Money.Idr(10m) - Money.Idr(4m)).Amount.Should().Be(6m);
    }

    [Fact]
    public void Comparison_uses_the_amount()
    {
        (Money.Idr(100m) < Money.Idr(200m)).Should().BeTrue();
        (Money.Idr(200m) > Money.Idr(100m)).Should().BeTrue();
        (Money.Idr(200m) >= Money.Idr(200m)).Should().BeTrue();
    }

    [Fact]
    public void Equality_includes_the_currency()
    {
        Money.Idr(100m).Should().Be(Money.Idr(100m));
        Money.Idr(100m).Should().NotBe(Money.Of(100m, "USD"));
    }
}
