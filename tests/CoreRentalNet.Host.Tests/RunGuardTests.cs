using System.Security.Claims;
using AwesomeAssertions;
using CoreRentalNet.Host.AiBuilder;
using Xunit;

namespace CoreRentalNet.Host.Tests;

/// <summary>AIWB-22 and AIWB-23: one run in flight per customer, released on every exit path.</summary>
public sealed class RunGuardTests
{
    private static readonly ClaimsPrincipal Dewi = Customer("auth0|dewi");
    private static readonly ClaimsPrincipal Adi = Customer("auth0|adi");

    [Fact] // AIWB-22
    public void A_customer_who_already_has_a_run_in_flight_is_refused_a_second_one()
    {
        var guard = new RunGuard();

        using var first = guard.TryBegin(Dewi);

        first.Should().NotBeNull();
        guard.TryBegin(Dewi).Should().BeNull("a run is paid, so one at a time");
    }

    [Fact] // AIWB-22
    public void One_customer_s_run_does_not_block_another()
    {
        var guard = new RunGuard();

        using var dewi = guard.TryBegin(Dewi);
        using var adi = guard.TryBegin(Adi);

        dewi.Should().NotBeNull();
        adi.Should().NotBeNull();
    }

    [Fact] // AIWB-23
    public void The_guard_is_released_when_the_run_ends_normally()
    {
        var guard = new RunGuard();

        using (guard.TryBegin(Dewi))
        {
        }

        guard.TryBegin(Dewi).Should().NotBeNull();
    }

    [Fact] // AIWB-23, the path that wedges a customer out if it is missed
    public async Task The_guard_is_released_when_the_run_throws()
    {
        var guard = new RunGuard();

        var thrown = async () =>
        {
            using var lease = guard.TryBegin(Dewi);

            await Task.Yield();

            throw new InvalidOperationException("the stream failed");
        };

        await thrown.Should().ThrowAsync<InvalidOperationException>();

        guard.TryBegin(Dewi).Should().NotBeNull("a faulted run must not hold its customer's slot");
    }

    [Fact] // AIWB-23, cancellation
    public async Task The_guard_is_released_when_the_run_is_cancelled()
    {
        var guard = new RunGuard();
        using var stopping = new CancellationTokenSource();

        var cancelled = async () =>
        {
            using var lease = guard.TryBegin(Dewi);

            await stopping.CancelAsync();
            await Task.Delay(Timeout.Infinite, stopping.Token);
        };

        await cancelled.Should().ThrowAsync<OperationCanceledException>();

        guard.TryBegin(Dewi).Should().NotBeNull("stopping a run is how a customer gets to start another");
    }

    [Fact] // AIWB-23
    public void Releasing_the_same_lease_twice_does_not_release_a_later_run()
    {
        var guard = new RunGuard();

        var first = guard.TryBegin(Dewi);
        first!.Dispose();

        using var second = guard.TryBegin(Dewi);
        second.Should().NotBeNull();

        // A failure path that also releases must not free a slot a second run is holding.
        first.Dispose();

        guard.TryBegin(Dewi).Should().BeNull();
    }

    [Fact]
    public void An_account_with_nothing_stable_to_key_on_cannot_run()
    {
        // Refused rather than keyed on a shared empty string, which would let one account's run block
        // every other such account - or, worse, let all of them run at once under one key.
        var guard = new RunGuard();
        var anonymous = new ClaimsPrincipal(new ClaimsIdentity());

        guard.TryBegin(anonymous).Should().BeNull();
    }

    private static ClaimsPrincipal Customer(string identifier)
        => new(new ClaimsIdentity(
            [new Claim(ClaimTypes.NameIdentifier, identifier)],
            authenticationType: "test"));
}
