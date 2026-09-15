using AwesomeAssertions;
using CoreRentalNet.BuildingBlocks.Domain;
using CoreRentalNet.Modules.Workspace.Application.Workspace.Rules;
using CoreRentalNet.Modules.Workspace.Application.Workspace.Services;
using CoreRentalNet.Modules.Workspace.Domain;
using Xunit;
using WorkspaceDraft = CoreRentalNet.Modules.Workspace.Domain.Workspace;
using CoreRentalNet.BuildingBlocks.Application;

namespace CoreRentalNet.BehaviourLock;

/// <summary>
/// Stage-0 behaviour lock. Every assertion here pins an exact, user-visible refusal
/// message: <c>WorkspaceSession.AttemptAsync</c> returns <c>DomainRuleViolationException.Message</c>
/// to the screen, so the wording is contract, not implementation detail.
/// </summary>
/// <remarks>
/// Repointed, not replaced, when the rules moved from the aggregate to <see cref="WorkspaceService"/>
/// in stage 3. The invocation changed; not one expected message did. A change to an expected string is
/// a change to the requirement.
/// </remarks>
public sealed class WorkspaceRuleLockTests
{
    private static readonly IWorkspaceService Service = new WorkspaceService(new SlotRuleProvider());

    private static WorkspaceDraft NewDraft() => new()
    {
        Id = WorkspaceId.New(),
        DraftTokenHash = new OpaqueTokenService().HashOf("behaviour-lock"),
        State = DraftState.Draft,
        Version = 1,
        Assignments = [],
    };

    [Fact]
    public void A_fourth_monitor_is_refused_with_the_slot_capacity_message()
    {
        var draft = NewDraft();
        Service.Assign(draft, SlotId.Monitor, "MON0001", 3);

        var act = () => Service.Assign(draft, SlotId.Monitor, "MON0001");

        act.Should().Throw<DomainRuleViolationException>()
            .WithMessage("The Monitor slot holds at most 3 units, and already holds 3.");
    }

    [Fact]
    public void A_quantity_below_one_on_assign_is_refused()
    {
        var draft = NewDraft();

        var act = () => Service.Assign(draft, SlotId.Monitor, "MON0001", 0);

        act.Should().Throw<DomainRuleViolationException>()
            .WithMessage("Quantity must be at least 1, but was 0.");
    }

    [Fact]
    public void Changing_the_quantity_of_a_product_the_slot_does_not_hold_is_refused()
    {
        var draft = NewDraft();

        var act = () => Service.ChangeQuantity(draft, SlotId.Monitor, "MON0001", 1);

        act.Should().Throw<DomainRuleViolationException>()
            .WithMessage("The Monitor slot does not hold MON0001, so its quantity cannot change.");
    }

    [Fact]
    public void Changing_a_quantity_past_the_capacity_is_refused()
    {
        var draft = NewDraft();
        Service.Assign(draft, SlotId.Monitor, "MON0001", 2);

        var act = () => Service.ChangeQuantity(draft, SlotId.Monitor, "MON0001", 5);

        act.Should().Throw<DomainRuleViolationException>()
            .WithMessage("The Monitor slot holds at most 3 units, but 5 were requested.");
    }

    [Fact]
    public void A_negative_quantity_is_refused()
    {
        var draft = NewDraft();
        Service.Assign(draft, SlotId.Monitor, "MON0001");

        var act = () => Service.ChangeQuantity(draft, SlotId.Monitor, "MON0001", -1);

        act.Should().Throw<DomainRuleViolationException>()
            .WithMessage("Quantity cannot be negative, but was -1.");
    }

    [Fact]
    public void A_delivery_address_shorter_than_five_characters_is_refused()
    {
        var draft = NewDraft();

        var act = () => Service.SetDeliveryAddress(draft, "Vila");

        act.Should().Throw<DomainRuleViolationException>()
            .WithMessage("A delivery address needs at least 5 characters.");
    }

    [Fact]
    public void A_delivery_address_longer_than_two_hundred_characters_is_refused()
    {
        var draft = NewDraft();

        var act = () => Service.SetDeliveryAddress(draft, new string('a', 201));

        act.Should().Throw<DomainRuleViolationException>()
            .WithMessage("A delivery address cannot be longer than 200 characters.");
    }

    [Fact]
    public void A_workspace_cannot_be_turned_into_an_order_twice()
    {
        var draft = NewDraft();
        Service.MarkConverted(draft);

        var act = () => Service.MarkConverted(draft);

        act.Should().Throw<DomainRuleViolationException>()
            .WithMessage("This workspace has already been turned into an order.");
    }

    [Fact]
    public void A_converted_workspace_refuses_further_changes()
    {
        var draft = NewDraft();
        Service.MarkConverted(draft);

        var act = () => Service.Assign(draft, SlotId.Desk, "DSK0001");

        act.Should().Throw<DomainRuleViolationException>()
            .WithMessage("A workspace that has been turned into an order can no longer be changed.");
    }

    [Fact]
    public void A_slot_assignment_beyond_its_capacity_is_refused_at_the_service()
    {
        // The guard used to sit in the SlotAssignment constructor. It now sits in the one service that
        // writes an assignment, and the invariant it protects — capacity — is still refused.
        var draft = NewDraft();

        var act = () => Service.Assign(draft, SlotId.Monitor, "MON0001", 5);

        act.Should().Throw<DomainRuleViolationException>()
            .WithMessage("The Monitor slot holds at most 3 units, and already holds 0.");
    }

    [Fact]
    public void An_empty_workspace_id_is_refused()
    {
        var act = () => WorkspaceId.From(Guid.Empty);

        act.Should().Throw<DomainRuleViolationException>()
            .WithMessage("A workspace id cannot be empty.");
    }
}
