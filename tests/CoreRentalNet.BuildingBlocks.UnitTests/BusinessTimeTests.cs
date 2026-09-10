using AwesomeAssertions;
using CoreRentalNet.BuildingBlocks.Domain;
using Microsoft.Extensions.Time.Testing;
using Xunit;

namespace CoreRentalNet.BuildingBlocks.UnitTests;

public sealed class BusinessTimeTests
{
    [Fact] // SC-09
    public void An_early_morning_order_in_bali_anchors_to_the_bali_date_not_the_utc_one()
    {
        // 01:00 on 1 February in Denpasar is 17:00 on 31 January UTC.
        var instant = new DateTimeOffset(2026, 1, 31, 17, 0, 0, TimeSpan.Zero);

        BusinessTime.BusinessDate(instant).Should().Be(new DateOnly(2026, 2, 1));
    }

    [Fact] // SC-09
    public void A_late_evening_order_in_utc_still_anchors_to_the_bali_date()
    {
        // 23:30 on 1 February UTC is 07:30 on 2 February in Denpasar.
        var instant = new DateTimeOffset(2026, 2, 1, 23, 30, 0, TimeSpan.Zero);

        BusinessTime.BusinessDate(instant).Should().Be(new DateOnly(2026, 2, 2));
    }

    [Fact] // SC-09
    public void The_offset_is_exactly_eight_hours_and_never_shifts()
    {
        BusinessTime.UtcOffset.Should().Be(TimeSpan.FromHours(8));

        // Indonesia observes no daylight saving, so January and July agree.
        BusinessTime.BusinessDate(new DateTimeOffset(2026, 1, 1, 16, 0, 0, TimeSpan.Zero)).Should().Be(new DateOnly(2026, 1, 2));
        BusinessTime.BusinessDate(new DateTimeOffset(2026, 7, 1, 16, 0, 0, TimeSpan.Zero)).Should().Be(new DateOnly(2026, 7, 2));
    }

    [Fact]
    public void Today_comes_from_the_injected_clock()
    {
        var clock = new FakeTimeProvider(new DateTimeOffset(2026, 3, 31, 19, 0, 0, TimeSpan.Zero));

        BusinessTime.Today(clock).Should().Be(new DateOnly(2026, 4, 1));
    }

    [Fact]
    public void Month_arithmetic_clamps_and_then_returns_to_the_anchor_day()
    {
        var anchor = new DateOnly(2026, 1, 31);

        BusinessTime.AddMonths(anchor, 1).Should().Be(new DateOnly(2026, 2, 28));
        BusinessTime.AddMonths(anchor, 2).Should().Be(new DateOnly(2026, 3, 31));
        BusinessTime.AddMonths(anchor, 3).Should().Be(new DateOnly(2026, 4, 30));
        BusinessTime.AddMonths(anchor, 12).Should().Be(new DateOnly(2027, 1, 31));
    }
}
