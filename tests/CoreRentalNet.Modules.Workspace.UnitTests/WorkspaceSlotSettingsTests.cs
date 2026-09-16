using AwesomeAssertions;
using CoreRentalNet.BuildingBlocks.Application;
using CoreRentalNet.BuildingBlocks.Domain;
using CoreRentalNet.Modules.Workspace.Application.Rules;
using CoreRentalNet.Modules.Workspace.Application.Services;
using CoreRentalNet.Modules.Workspace.Domain;
using Xunit;
using WorkspaceDraft = CoreRentalNet.Modules.Workspace.Domain.Workspace;

namespace CoreRentalNet.Modules.Workspace.UnitTests;

/// <summary>
/// The capacities are configuration now, so these prove the shipped defaults are the table the
/// requirement pins, and that an override reaches the rule the service enforces.
/// </summary>
public sealed class WorkspaceSlotSettingsTests
{
    [Fact]
    public void The_shipped_defaults_are_the_table_the_requirement_pins()
    {
        var provider = new SlotRuleProvider(new WorkspaceSlotSettings());

        provider.All.Select(rule => (rule.Slot, rule.MaxQuantity)).Should().BeEquivalentTo(
        [
            (SlotId.Desk, 1),
            (SlotId.Chair, 1),
            (SlotId.Monitor, 3),
            (SlotId.Lamp, 1),
            (SlotId.Plant, 1),
            (SlotId.CoffeeStation, 1),
            (SlotId.RelaxZone, 1),
        ]);
        provider.TotalCapacity.Should().Be(9);
    }

    [Fact]
    public void A_configured_capacity_replaces_the_default_everywhere()
    {
        var provider = new SlotRuleProvider(new WorkspaceSlotSettings(Monitor: 5));

        provider.For(SlotId.Monitor).MaxQuantity.Should().Be(5);
        provider.TotalCapacity.Should().Be(11);
    }

    [Fact]
    public void A_configured_capacity_is_what_the_service_enforces()
    {
        var service = new WorkspaceService(new SlotRuleProvider(new WorkspaceSlotSettings(Monitor: 5)));
        var draft = NewDraft();
        service.Assign(draft, SlotId.Monitor, "MON0001", 5);

        var act = () => service.ChangeQuantity(draft, SlotId.Monitor, "MON0001", 6);

        act.Should().Throw<DomainRuleViolationException>()
            .WithMessage("The Monitor slot holds at most 5 units, but 6 were requested.");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void A_capacity_below_one_is_refused_and_names_the_slot(int capacity)
    {
        var act = () => new SlotRuleProvider(new WorkspaceSlotSettings(Monitor: capacity));

        act.Should().Throw<DomainRuleViolationException>()
            .WithMessage($"The Monitor slot must accept at least one unit, but {capacity} was configured.");
    }

    private static WorkspaceDraft NewDraft() => new()
    {
        Id = WorkspaceId.New(),
        DraftTokenHash = new OpaqueTokenService().HashOf("slot-settings"),
        State = DraftState.Draft,
        Version = 1,
        Assignments = [],
    };
}
