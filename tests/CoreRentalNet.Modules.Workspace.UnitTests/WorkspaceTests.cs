using AwesomeAssertions;
using CoreRentalNet.BuildingBlocks.Domain;
using CoreRentalNet.Modules.Workspace.Application.Queries.Services;
using CoreRentalNet.Modules.Workspace.Application.Workspace.Rules;
using CoreRentalNet.Modules.Workspace.Application.Workspace.Services;
using CoreRentalNet.Modules.Workspace.Domain;
using Xunit;
using CoreRentalNet.BuildingBlocks.Application;

namespace CoreRentalNet.Modules.Workspace.UnitTests;

/// <summary>
/// The workspace's rules, now enforced by <see cref="WorkspaceService"/> against a plain record
///. Every assertion is the same as it was against the aggregate; only the call site moved.
/// </summary>
public sealed class WorkspaceTests
{
    private static readonly ISlotRuleProvider Rules = new SlotRuleProvider();
    private static readonly IWorkspaceService Service = new WorkspaceService(Rules);
    private static readonly IWorkspaceQueryService Queries = new WorkspaceQueryService(Rules);

    private static Domain.Workspace NewDraft() => new()
    {
        Id = WorkspaceId.New(),
        DraftTokenHash = new OpaqueTokenService().HashOf("raw"),
        State = DraftState.Draft,
        Version = 1,
        Assignments = [],
    };

    [Fact] // WS-01
    public void Assigning_a_product_fills_its_slot_with_one_unit()
    {
        var workspace = NewDraft();

        Service.Assign(workspace, SlotId.Chair, "cha0001");

        Queries.AssignmentsFor(workspace, SlotId.Chair).Single().Sku.Should().Be("CHA0001");
        Queries.AssignmentsFor(workspace, SlotId.Chair).Single().Quantity.Should().Be(1);
        Queries.TotalUnits(workspace).Should().Be(1);
    }

    [Fact] // WS-02
    public void A_single_capacity_slot_is_replaced_rather_than_accumulated()
    {
        var workspace = NewDraft();
        Service.Assign(workspace, SlotId.Chair, "CHA0001");

        Service.Assign(workspace, SlotId.Chair, "CHA0002");

        Queries.AssignmentsFor(workspace, SlotId.Chair).Single().Sku.Should().Be("CHA0002");
        Queries.AssignmentsFor(workspace, SlotId.Chair).Single().Quantity.Should().Be(1);
        Queries.TotalUnits(workspace).Should().Be(1);
    }

    [Fact] // WS-03
    public void Monitors_accumulate_up_to_the_capacity()
    {
        var workspace = NewDraft();

        Service.Assign(workspace, SlotId.Monitor, "MON0001");
        Service.Assign(workspace, SlotId.Monitor, "MON0001");
        Service.Assign(workspace, SlotId.Monitor, "MON0001");

        Queries.AssignmentsFor(workspace, SlotId.Monitor).Single().Quantity.Should().Be(3);
        Queries.TotalUnits(workspace).Should().Be(3);
    }

    [Fact] // WS-04
    public void A_fourth_monitor_is_refused_and_changes_nothing()
    {
        var workspace = NewDraft();
        Service.Assign(workspace, SlotId.Monitor, "MON0001", 3);
        var versionBefore = workspace.Version;

        var action = () => Service.Assign(workspace, SlotId.Monitor, "MON0001");

        action.Should().Throw<DomainRuleViolationException>().WithMessage("*at most 3*");
        Queries.AssignmentsFor(workspace, SlotId.Monitor).Single().Quantity.Should().Be(3);
        workspace.Version.Should().Be(versionBefore, "a refused command must not change the record");
    }

    [Fact] // WS-08
    public void Removing_a_filled_slot_empties_it_and_removing_an_empty_slot_is_a_no_op()
    {
        var workspace = NewDraft();
        Service.Assign(workspace, SlotId.Plant, "PLT0001");

        Service.Remove(workspace, SlotId.Plant).Should().BeTrue();
        Queries.AssignmentsFor(workspace, SlotId.Plant).Should().BeEmpty();
        Queries.IsEmpty(workspace).Should().BeTrue();

        Service.Remove(workspace, SlotId.Plant).Should().BeFalse();
    }

    [Fact] // WS-09
    public void A_quantity_of_zero_empties_the_slot()
    {
        var workspace = NewDraft();
        Service.Assign(workspace, SlotId.Monitor, "MON0001", 2);

        Service.ChangeQuantity(workspace, SlotId.Monitor, "MON0001", 0);

        Queries.AssignmentsFor(workspace, SlotId.Monitor).Should().BeEmpty();
    }

    [Fact] // WS-09
    public void Changing_the_quantity_of_an_empty_slot_is_refused()
    {
        var workspace = NewDraft();

        var action = () => Service.ChangeQuantity(workspace, SlotId.Monitor, "MON0001", 2);

        // The message names what is missing, because with three monitors the slot can hold other
        // products and "empty" would not say which one the customer tried to count.
        action.Should().Throw<DomainRuleViolationException>().WithMessage("*does not hold MON0001*");
    }

    [Fact] // WS-10
    public void A_quantity_beyond_capacity_is_refused()
    {
        var workspace = NewDraft();
        Service.Assign(workspace, SlotId.Monitor, "MON0001");

        var action = () => Service.ChangeQuantity(workspace, SlotId.Monitor, "MON0001", 4);

        action.Should().Throw<DomainRuleViolationException>().WithMessage("*at most 3*");
    }

    [Fact]
    public void A_negative_quantity_is_refused()
    {
        var workspace = NewDraft();
        Service.Assign(workspace, SlotId.Monitor, "MON0001");

        var action = () => Service.ChangeQuantity(workspace, SlotId.Monitor, "MON0001", -1);

        action.Should().Throw<DomainRuleViolationException>();
    }

    [Fact]
    public void Assigning_zero_units_is_refused()
    {
        var workspace = NewDraft();

        var action = () => Service.Assign(workspace, SlotId.Monitor, "MON0001", 0);

        action.Should().Throw<DomainRuleViolationException>();
    }

    [Fact] // ADDR-02
    public void A_blank_delivery_address_clears_it_rather_than_storing_whitespace()
    {
        var workspace = NewDraft();
        Service.SetDeliveryAddress(workspace, "Villa Lotus, Canggu");

        Service.SetDeliveryAddress(workspace, "   ");

        workspace.DeliveryAddress.Should().BeNull();
    }

    [Fact] // ADDR-03
    public void A_too_short_address_is_refused_at_both_bounds()
    {
        var workspace = NewDraft();

        var tooShort = () => Service.SetDeliveryAddress(workspace, "Vila");
        tooShort.Should().Throw<DomainRuleViolationException>().WithMessage("*at least 5*");

        var tooLong = () => Service.SetDeliveryAddress(workspace, new string('a', 201));
        tooLong.Should().Throw<DomainRuleViolationException>().WithMessage("*200*");
    }

    [Fact] // ADDR-05
    public void Line_endings_and_blank_lines_in_the_address_are_normalised()
    {
        var workspace = NewDraft();

        Service.SetDeliveryAddress(workspace, "  Villa Lotus  \r\n\r\n  Canggu   \r\n");

        workspace.DeliveryAddress.Should().Be("Villa Lotus\nCanggu");
    }

    [Fact]
    public void A_five_character_address_is_accepted()
    {
        var workspace = NewDraft();

        Service.SetDeliveryAddress(workspace, "12345");

        workspace.DeliveryAddress.Should().Be("12345");
    }

    [Fact]
    public void Every_mutation_bumps_the_concurrency_version()
    {
        var workspace = NewDraft();
        var start = workspace.Version;

        Service.Assign(workspace, SlotId.Chair, "CHA0001");
        Service.Assign(workspace, SlotId.Desk, "DSK0001");
        Service.ChangeQuantity(workspace, SlotId.Chair, "CHA0001", 1);
        Service.SetDeliveryAddress(workspace, "Villa Lotus, Canggu");

        workspace.Version.Should().Be(start + 4);
    }

    [Fact] // CO-11
    public void A_converted_workspace_refuses_every_further_change()
    {
        var workspace = NewDraft();
        Service.Assign(workspace, SlotId.Chair, "CHA0001");
        Service.MarkConverted(workspace);

        Queries.IsConverted(workspace).Should().BeTrue();
        ((Action)(() => Service.Assign(workspace, SlotId.Desk, "DSK0001"))).Should().Throw<DomainRuleViolationException>();
        ((Action)(() => Service.Remove(workspace, SlotId.Chair))).Should().Throw<DomainRuleViolationException>();
        ((Action)(() => Service.ChangeQuantity(workspace, SlotId.Chair, "CHA0001", 1))).Should().Throw<DomainRuleViolationException>();
        ((Action)(() => Service.SetDeliveryAddress(workspace, "Villa Lotus, Canggu"))).Should().Throw<DomainRuleViolationException>();
    }

    [Fact] // CO-11
    public void Converting_twice_is_refused()
    {
        var workspace = NewDraft();
        Service.MarkConverted(workspace);

        var action = () => Service.MarkConverted(workspace);

        action.Should().Throw<DomainRuleViolationException>().WithMessage("*already*");
    }

    [Fact]
    public void A_workspace_id_cannot_be_empty()
    {
        var action = () => WorkspaceId.From(Guid.Empty);

        action.Should().Throw<DomainRuleViolationException>();
    }

    [Fact] // WS-15
    public void Adding_a_second_monitor_of_a_different_model_keeps_the_first()
    {
        var workspace = NewDraft();

        Service.Assign(workspace, SlotId.Monitor, "MON0001");
        Service.Assign(workspace, SlotId.Monitor, "MON0002");

        // Two monitors are in the slot and the first is one of them. Assigning a different product
        // into a multi-capacity slot adds a unit; it does not change what the customer already chose.
        Queries.AssignmentsFor(workspace, SlotId.Monitor).Select(assignment => assignment.Sku)
            .Should().BeEquivalentTo(["MON0001", "MON0002"]);
        Queries.TotalUnits(workspace).Should().Be(2);
    }
}
