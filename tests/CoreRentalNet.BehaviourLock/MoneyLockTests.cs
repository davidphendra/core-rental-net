using System.Globalization;
using AwesomeAssertions;
using CoreRentalNet.BuildingBlocks.Application;
using CoreRentalNet.BuildingBlocks.Domain;
using Xunit;

namespace CoreRentalNet.BehaviourLock;

/// <summary>
/// Stage-0 behaviour lock. Every assertion here pins an exact, user-visible refusal
/// message.
/// </summary>
/// <remarks>
/// Repointed, not relaxed, when the money rules moved from the <c>Money</c> value object to
/// <see cref="MoneyService"/> in stage 5. The invocation changed; not one expected message did.
/// </remarks>
public sealed class MoneyLockTests
{
    private static readonly IMoneyService Service = new MoneyService();

    private static Money Idr(decimal amount) => new(amount, Currencies.Idr);

    [Fact]
    public void A_negative_amount_is_refused()
    {
        var act = () => Service.Idr(-1m);

        act.Should().Throw<DomainRuleViolationException>()
            .WithMessage("A money amount cannot be negative, but was -1.");
    }

    [Fact]
    public void Zero_is_allowed()
    {
        Idr(0m).Amount.Should().Be(0m);
    }

    [Fact]
    public void Combining_two_currencies_is_refused()
    {
        var act = () => Service.Add(Idr(100m), new Money(1m, "USD"));

        act.Should().Throw<DomainRuleViolationException>()
            .WithMessage("Cannot combine IDR with USD.");
    }

    [Fact]
    public void Subtracting_into_the_negative_is_refused()
    {
        var act = () => Service.Subtract(Idr(100m), Idr(200m));

        act.Should().Throw<DomainRuleViolationException>()
            .WithMessage("Cannot subtract 200 IDR from 100 IDR: the result would be negative.");
    }

    [Fact]
    public void Multiplying_by_a_negative_quantity_is_refused()
    {
        var act = () => Service.Times(Idr(100m), -1);

        act.Should().Throw<DomainRuleViolationException>()
            .WithMessage("Cannot multiply money by a negative quantity (-1).");
    }

    [Fact]
    public void Summing_nothing_is_refused()
    {
        var act = () => Service.Sum([]);

        act.Should().Throw<DomainRuleViolationException>()
            .WithMessage("Cannot sum an empty sequence of money values.");
    }

    [Theory]
    [InlineData(0.005, 0.01)]
    [InlineData(0.015, 0.02)]
    [InlineData(2.675, 2.68)]
    [InlineData(1.004, 1.00)]
    public void Rounding_happens_once_away_from_zero(decimal value, decimal rounded)
    {
        Service.RoundAmount(value).Should().Be(rounded);
    }

    [Fact]
    public void Idr_is_displayed_with_no_decimals_and_local_grouping()
    {
        // Re-pointed when the format moved off a hard-coded prefix and onto the culture: the
        // culture is what puts "Rp" before a zero-decimal amount, exactly as before. The browser
        // suite pins the page this is drawn on, and the architecture suite pins the culture that
        // appsettings.json ships.
        Idr(400_000m).Amount.ToString("C", new CultureInfo("en-ID", useUserOverride: false)).Should().Be("Rp400.000");
    }
}
