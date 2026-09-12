using AwesomeAssertions;
using CoreRentalNet.BuildingBlocks.Domain;
using CoreRentalNet.Host.Presentation;
using CoreRentalNet.Modules.Workspace.Application.Workspace;
using CoreRentalNet.Modules.Workspace.Domain;
using Xunit;

namespace CoreRentalNet.Host.Tests;

/// <summary>
/// The rule behind the button on the review page: the workspace has to be rentable, and the address
/// has to be there.
/// </summary>
public sealed class CheckoutGateTests
{
    [Fact] // GATE-01
    public void Without_a_workspace_there_is_nothing_to_order()
    {
        CheckoutGate.CanPlaceOrder(null).Should().BeFalse();
        CheckoutGate.BlockingReason(null).Should().Be(CheckoutGate.WorkspaceRequired);
    }

    [Fact] // GATE-02
    public void A_workspace_that_cannot_be_rented_says_why_in_the_domains_own_words()
    {
        var missingAChair = View(missing: ["Chair"]);

        CheckoutGate.CanPlaceOrder(missingAChair).Should().BeFalse();
        CheckoutGate.BlockingReason(missingAChair).Should().Be("A chair is required before you can rent.");
    }

    [Fact] // GATE-03
    public void An_empty_workspace_cannot_be_ordered()
    {
        var empty = View(empty: true);

        CheckoutGate.CanPlaceOrder(empty).Should().BeFalse();
        CheckoutGate.BlockingReason(empty).Should().Be("Add an item to your workspace first.");
    }

    [Fact] // GATE-04
    public void A_rentable_workspace_still_needs_an_address()
    {
        var noAddress = View(address: null);

        CheckoutGate.CanPlaceOrder(noAddress).Should().BeFalse();
        CheckoutGate.BlockingReason(noAddress).Should().Be(CheckoutGate.AddressRequired);
    }

    [Theory] // GATE-05
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void An_address_of_nothing_is_not_an_address(string? address)
    {
        var nothing = View(address: address);

        CheckoutGate.CanPlaceOrder(nothing).Should().BeFalse();
        CheckoutGate.BlockingReason(nothing).Should().Be(CheckoutGate.AddressRequired);
    }

    [Fact] // GATE-07
    public void An_address_that_has_only_been_typed_counts_until_it_is_saved()
    {
        var nothingStored = View(address: null);

        CheckoutGate.CanPlaceOrder(nothingStored, "Villa Lotus, Canggu").Should().BeTrue();
        CheckoutGate.BlockingReason(nothingStored, "Villa Lotus, Canggu").Should().BeNull();

        // And nothing typed is still nothing, spaces included.
        CheckoutGate.CanPlaceOrder(nothingStored, "   ").Should().BeFalse();
        CheckoutGate.BlockingReason(nothingStored, "   ").Should().Be(CheckoutGate.AddressRequired);
    }

    [Fact] // GATE-06
    public void A_rentable_workspace_with_an_address_can_be_ordered()
    {
        var ready = View(address: "Villa Lotus, Canggu");

        CheckoutGate.CanPlaceOrder(ready).Should().BeTrue();
        CheckoutGate.BlockingReason(ready).Should().BeNull();
    }

    /// <summary>
    /// A view with the flags the gate reads, rather than a whole workspace: the gate decides from what
    /// the workspace already worked out about itself, so that is what it is handed.
    /// </summary>
    private static WorkspaceView View(string? address = null, bool empty = false, string[]? missing = null)
        => new(Guid.NewGuid(), DraftState.Draft, Version: 1, Slots: [], address, TotalUnits: 0, empty,
               new WorkspaceQuote([], Money.Idr(0m)), missing ?? []);
}
