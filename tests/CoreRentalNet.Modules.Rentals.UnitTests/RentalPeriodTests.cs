using AwesomeAssertions;
using CoreRentalNet.BuildingBlocks.Domain;
using CoreRentalNet.Modules.Rentals.Domain;
using Xunit;

namespace CoreRentalNet.Modules.Rentals.UnitTests;

public sealed class RentalPeriodTests
{
    [Fact] // SC-07
    public void An_order_on_the_thirty_first_clamps_in_february_and_returns_to_the_thirty_first()
    {
        var anchor = new DateOnly(2026, 1, 31);

        RentalPeriod.For(anchor, 0).Start.Should().Be(new DateOnly(2026, 1, 31));
        RentalPeriod.For(anchor, 0).EndExclusive.Should().Be(new DateOnly(2026, 2, 28));
        RentalPeriod.For(anchor, 1).Start.Should().Be(new DateOnly(2026, 2, 28));
        RentalPeriod.For(anchor, 1).EndExclusive.Should().Be(new DateOnly(2026, 3, 31));
        RentalPeriod.For(anchor, 2).Start.Should().Be(new DateOnly(2026, 3, 31));
        RentalPeriod.For(anchor, 2).EndExclusive.Should().Be(new DateOnly(2026, 4, 30));
        RentalPeriod.For(anchor, 3).Start.Should().Be(new DateOnly(2026, 4, 30));
        RentalPeriod.For(anchor, 3).EndExclusive.Should().Be(new DateOnly(2026, 5, 31));
    }

    [Fact] // SC-08
    public void The_billing_date_never_drifts_across_a_year()
    {
        var anchor = new DateOnly(2026, 1, 31);

        var starts = Enumerable.Range(0, 12).Select(index => RentalPeriod.For(anchor, index).Start).ToArray();

        starts.Should().Contain(new DateOnly(2026, 1, 31));
        starts.Should().Contain(new DateOnly(2026, 3, 31));
        starts.Should().Contain(new DateOnly(2026, 5, 31));
        starts.Should().Contain(new DateOnly(2026, 7, 31));
        starts.Should().Contain(new DateOnly(2026, 8, 31));
        starts.Should().Contain(new DateOnly(2026, 10, 31));
        starts.Should().Contain(new DateOnly(2026, 12, 31));

        // Every period starts where the previous one ended: no gap, no overlap.
        for (var index = 1; index < starts.Length; index++)
        {
            starts[index].Should().Be(RentalPeriod.For(anchor, index - 1).EndExclusive);
        }
    }

    [Fact] // SC-07
    public void A_month_end_anchor_works_in_a_leap_year_too()
    {
        var anchor = new DateOnly(2028, 1, 31);

        RentalPeriod.For(anchor, 1).Start.Should().Be(new DateOnly(2028, 2, 29), "2028 is a leap year");
    }

    [Fact]
    public void A_period_contains_its_first_day_but_not_its_last()
    {
        var period = RentalPeriod.For(new DateOnly(2026, 1, 10), 0);

        period.Contains(new DateOnly(2026, 1, 10)).Should().BeTrue();
        period.Contains(new DateOnly(2026, 2, 9)).Should().BeTrue();
        period.Contains(new DateOnly(2026, 2, 10)).Should().BeFalse("that day belongs to the next period");
        period.Contains(new DateOnly(2026, 1, 9)).Should().BeFalse();
        period.LastDay.Should().Be(new DateOnly(2026, 2, 9));
    }

    [Fact]
    public void Which_period_a_date_falls_in_is_answered_from_the_anchor()
    {
        var anchor = new DateOnly(2026, 1, 10);

        RenewalPolicy.PeriodIndexContaining(anchor, new DateOnly(2026, 1, 10)).Should().Be(0);
        RenewalPolicy.PeriodIndexContaining(anchor, new DateOnly(2026, 2, 9)).Should().Be(0);
        RenewalPolicy.PeriodIndexContaining(anchor, new DateOnly(2026, 2, 10)).Should().Be(1);
        RenewalPolicy.PeriodIndexContaining(anchor, new DateOnly(2026, 4, 15)).Should().Be(3);
    }

    [Fact]
    public void A_date_before_the_anchor_is_refused()
    {
        var action = () => RenewalPolicy.PeriodIndexContaining(new DateOnly(2026, 1, 10), new DateOnly(2026, 1, 9));

        action.Should().Throw<DomainRuleViolationException>();
    }

    [Fact]
    public void A_negative_period_index_is_refused()
    {
        var action = () => RentalPeriod.For(new DateOnly(2026, 1, 10), -1);

        action.Should().Throw<DomainRuleViolationException>();
    }

    [Fact]
    public void A_delivery_is_scheduled_two_days_after_the_order()
    {
        DeliveryPolicy.ScheduledFor(new DateOnly(2026, 1, 30)).Should().Be(new DateOnly(2026, 2, 1));
        DeliveryPolicy.LeadTimeDays.Should().Be(2);
    }
}
