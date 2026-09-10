using AwesomeAssertions;
using CoreRentalNet.BuildingBlocks.Domain;
using CoreRentalNet.Modules.Workspace.Domain;
using Xunit;

namespace CoreRentalNet.Modules.Workspace.UnitTests;

public sealed class WorkspaceTests
{
    private static Domain.Workspace NewDraft()
        => Domain.Workspace.CreateNew(WorkspaceId.New(), DraftToken.FromRawToken("raw").Hash);

    [Fact] // WS-01
    public void Assigning_a_product_fills_its_slot_with_one_unit()
    {
        var workspace = NewDraft();

        workspace.Assign(SlotId.Chair, "cha0001");

        workspace.AssignmentFor(SlotId.Chair)!.Sku.Should().Be("CHA0001");
        workspace.AssignmentFor(SlotId.Chair)!.Quantity.Should().Be(1);
        workspace.TotalUnits.Should().Be(1);
    }

    [Fact] // WS-02
    public void A_single_capacity_slot_is_replaced_rather_than_accumulated()
    {
        var workspace = NewDraft();
        workspace.Assign(SlotId.Chair, "CHA0001");

        workspace.Assign(SlotId.Chair, "CHA0002");

        workspace.AssignmentFor(SlotId.Chair)!.Sku.Should().Be("CHA0002");
        workspace.AssignmentFor(SlotId.Chair)!.Quantity.Should().Be(1);
        workspace.TotalUnits.Should().Be(1);
    }

    [Fact] // WS-03
    public void Monitors_accumulate_up_to_the_capacity()
    {
        var workspace = NewDraft();

        workspace.Assign(SlotId.Monitor, "MON0001");
        workspace.Assign(SlotId.Monitor, "MON0001");
        workspace.Assign(SlotId.Monitor, "MON0001");

        workspace.AssignmentFor(SlotId.Monitor)!.Quantity.Should().Be(3);
        workspace.TotalUnits.Should().Be(3);
    }

    [Fact] // WS-04
    public void A_fourth_monitor_is_refused_and_changes_nothing()
    {
        var workspace = NewDraft();
        workspace.Assign(SlotId.Monitor, "MON0001", 3);
        var versionBefore = workspace.Version;

        var action = () => workspace.Assign(SlotId.Monitor, "MON0001");

        action.Should().Throw<DomainRuleViolationException>().WithMessage("*at most 3*");
        workspace.AssignmentFor(SlotId.Monitor)!.Quantity.Should().Be(3);
        workspace.Version.Should().Be(versionBefore, "a refused command must not mutate the aggregate");
    }

    [Fact] // WS-08
    public void Removing_a_filled_slot_empties_it_and_removing_an_empty_slot_is_a_no_op()
    {
        var workspace = NewDraft();
        workspace.Assign(SlotId.Plant, "PLT0001");

        workspace.Remove(SlotId.Plant).Should().BeTrue();
        workspace.AssignmentFor(SlotId.Plant).Should().BeNull();
        workspace.IsEmpty.Should().BeTrue();

        workspace.Remove(SlotId.Plant).Should().BeFalse();
    }

    [Fact] // WS-09
    public void A_quantity_of_zero_empties_the_slot()
    {
        var workspace = NewDraft();
        workspace.Assign(SlotId.Monitor, "MON0001", 2);

        workspace.ChangeQuantity(SlotId.Monitor, 0);

        workspace.AssignmentFor(SlotId.Monitor).Should().BeNull();
    }

    [Fact] // WS-09
    public void Changing_the_quantity_of_an_empty_slot_is_refused()
    {
        var workspace = NewDraft();

        var action = () => workspace.ChangeQuantity(SlotId.Monitor, 2);

        action.Should().Throw<DomainRuleViolationException>().WithMessage("*empty*");
    }

    [Fact] // WS-10
    public void A_quantity_beyond_capacity_is_refused()
    {
        var workspace = NewDraft();
        workspace.Assign(SlotId.Monitor, "MON0001");

        var action = () => workspace.ChangeQuantity(SlotId.Monitor, 4);

        action.Should().Throw<DomainRuleViolationException>().WithMessage("*at most 3*");
    }

    [Fact]
    public void A_negative_quantity_is_refused()
    {
        var workspace = NewDraft();
        workspace.Assign(SlotId.Monitor, "MON0001");

        var action = () => workspace.ChangeQuantity(SlotId.Monitor, -1);

        action.Should().Throw<DomainRuleViolationException>();
    }

    [Fact]
    public void Assigning_zero_units_is_refused()
    {
        var workspace = NewDraft();

        var action = () => workspace.Assign(SlotId.Monitor, "MON0001", 0);

        action.Should().Throw<DomainRuleViolationException>();
    }

    [Fact] // ADDR-02
    public void A_blank_delivery_address_clears_it_rather_than_storing_whitespace()
    {
        var workspace = NewDraft();
        workspace.SetDeliveryAddress("Villa Lotus, Canggu");

        workspace.SetDeliveryAddress("   ");

        workspace.DeliveryAddress.Should().BeNull();
    }

    [Fact] // ADDR-03
    public void A_too_short_address_is_refused_at_both_bounds()
    {
        var workspace = NewDraft();

        var tooShort = () => workspace.SetDeliveryAddress("Vila");
        tooShort.Should().Throw<DomainRuleViolationException>().WithMessage("*at least 5*");

        var tooLong = () => workspace.SetDeliveryAddress(new string('a', 201));
        tooLong.Should().Throw<DomainRuleViolationException>().WithMessage("*200*");
    }

    [Fact] // ADDR-05
    public void Line_endings_and_blank_lines_in_the_address_are_normalised()
    {
        var workspace = NewDraft();

        workspace.SetDeliveryAddress("  Villa Lotus  \r\n\r\n  Canggu   \r\n");

        workspace.DeliveryAddress.Should().Be("Villa Lotus\nCanggu");
    }

    [Fact]
    public void A_five_character_address_is_accepted()
    {
        var workspace = NewDraft();

        workspace.SetDeliveryAddress("12345");

        workspace.DeliveryAddress.Should().Be("12345");
    }

    [Fact]
    public void Every_mutation_bumps_the_concurrency_version()
    {
        var workspace = NewDraft();
        var start = workspace.Version;

        workspace.Assign(SlotId.Chair, "CHA0001");
        workspace.Assign(SlotId.Desk, "DSK0001");
        workspace.ChangeQuantity(SlotId.Chair, 1);
        workspace.SetDeliveryAddress("Villa Lotus, Canggu");

        workspace.Version.Should().Be(start + 4);
    }

    [Fact] // CO-11
    public void A_converted_workspace_refuses_every_further_change()
    {
        var workspace = NewDraft();
        workspace.Assign(SlotId.Chair, "CHA0001");
        workspace.MarkConverted();

        workspace.IsConverted.Should().BeTrue();
        ((Action)(() => workspace.Assign(SlotId.Desk, "DSK0001"))).Should().Throw<DomainRuleViolationException>();
        ((Action)(() => workspace.Remove(SlotId.Chair))).Should().Throw<DomainRuleViolationException>();
        ((Action)(() => workspace.ChangeQuantity(SlotId.Chair, 1))).Should().Throw<DomainRuleViolationException>();
        ((Action)(() => workspace.SetDeliveryAddress("Villa Lotus, Canggu"))).Should().Throw<DomainRuleViolationException>();
    }

    [Fact] // CO-11
    public void Converting_twice_is_refused()
    {
        var workspace = NewDraft();
        workspace.MarkConverted();

        var action = () => workspace.MarkConverted();

        action.Should().Throw<DomainRuleViolationException>().WithMessage("*already*");
    }

    [Fact]
    public void A_workspace_cannot_be_created_with_an_empty_id()
    {
        var action = () => Domain.Workspace.CreateNew(default, DraftToken.FromRawToken("raw").Hash);

        action.Should().Throw<DomainRuleViolationException>();
    }
}
