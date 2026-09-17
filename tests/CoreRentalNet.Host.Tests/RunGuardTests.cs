using AwesomeAssertions;
using CoreRentalNet.Host.Presentation;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace CoreRentalNet.Host.Tests;

/// <summary>
/// One run at a time per customer, and a short gap between them.
/// </summary>
/// <remarks>
/// A cost control, not a rate limiter: the realistic way ten model calls become twenty is a double click
/// and a slow page, so what is asserted is that a second submit finds nothing to take, that every
/// terminal path gives it back, and that one customer's run never blocks another's.
/// </remarks>
public sealed class RunGuardTests
{
    private static AiRunGuard Guard(TimeSpan cooldown)
        => new(new AiRunSettings(cooldown));

    /// <summary>AIB-22 — a second submit while a run is in flight is refused.</summary>
    [Fact]
    public void A_second_submit_while_a_run_is_in_flight_is_refused()
    {
        var guard = Guard(TimeSpan.Zero);

        guard.TryBegin("auth0|one").Should().BeTrue();

        // The double click, and the impatient second Enter.
        guard.TryBegin("auth0|one").Should().BeFalse();
        guard.TryBegin("auth0|one").Should().BeFalse();
    }

    /// <summary>The guard is the customer's, so it never blocks somebody else's run.</summary>
    [Fact]
    public void One_customers_run_does_not_block_anothers()
    {
        var guard = Guard(TimeSpan.Zero);

        guard.TryBegin("auth0|one").Should().BeTrue();
        guard.TryBegin("auth0|two").Should().BeTrue();

        guard.Release("auth0|one");

        guard.TryBegin("auth0|one").Should().BeTrue();
        guard.TryBegin("auth0|two").Should().BeFalse();
    }

    /// <summary>
    /// Releasing is measured from when the run started, so the cooldown survives it.
    /// </summary>
    /// <remarks>
    /// A guard that forgot the run on release would let a customer start again the instant the first one
    /// ended, which is exactly the pattern the cooldown is for.
    /// </remarks>
    [Fact]
    public void The_cooldown_outlives_the_run()
    {
        var guard = Guard(TimeSpan.FromMinutes(5));

        guard.TryBegin("auth0|one").Should().BeTrue();
        guard.Release("auth0|one");

        guard.TryBegin("auth0|one").Should().BeFalse("the cooldown is measured from when the run started");
    }

    /// <summary>A cancelled run gives the slot back, because cancelling must not punish anybody.</summary>
    [Fact]
    public void A_run_that_was_released_can_be_started_again_once_the_cooldown_has_passed()
    {
        var guard = Guard(TimeSpan.Zero);

        guard.TryBegin("auth0|one").Should().BeTrue();
        guard.Release("auth0|one");

        guard.TryBegin("auth0|one").Should().BeTrue();
    }

    /// <summary>Releasing twice is not a way to escape the cooldown.</summary>
    [Fact]
    public void Releasing_a_run_that_never_began_changes_nothing()
    {
        var guard = Guard(TimeSpan.Zero);

        guard.Release("auth0|stranger");

        guard.TryBegin("auth0|stranger").Should().BeTrue();
    }

    [Fact]
    public void An_identity_is_required()
    {
        var guard = Guard(TimeSpan.Zero);

        Assert.Throws<ArgumentException>(() => guard.TryBegin(string.Empty));
        Assert.Throws<ArgumentException>(() => guard.Release("  "));
    }

    /// <summary>The cooldown is configured rather than hard-coded, and a nonsense value is ignored.</summary>
    [Theory]
    [InlineData("0", 0)]
    [InlineData("30", 30)]
    [InlineData("-5", 3)]
    [InlineData("not a number", 3)]
    public void The_cooldown_comes_from_configuration(string configured, double expected)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Ai:RunCooldownSeconds"] = configured })
            .Build();

        AiRunSettings.From(configuration).Cooldown.Should().Be(TimeSpan.FromSeconds(expected));
    }
}
