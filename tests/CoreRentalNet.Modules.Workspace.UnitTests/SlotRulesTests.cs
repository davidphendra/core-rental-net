using AwesomeAssertions;
using CoreRentalNet.Modules.Workspace.Application.Workspace.Rules;
using CoreRentalNet.Modules.Workspace.Domain;
using Xunit;

namespace CoreRentalNet.Modules.Workspace.UnitTests;

public sealed class SlotRulesTests
{
    private static readonly ISlotRuleProvider Rules = new SlotRuleProvider();

    [Fact] // SLOT-01
    public void The_slot_table_is_exactly_the_seven_agreed_slots()
    {
        Rules.All.Should().HaveCount(7);
        Rules.All.Select(rule => (rule.Slot, rule.MaxQuantity)).Should().BeEquivalentTo(
        [
            (SlotId.Desk, 1),
            (SlotId.Chair, 1),
            (SlotId.Monitor, 3),
            (SlotId.Lamp, 1),
            (SlotId.Plant, 1),
            (SlotId.CoffeeStation, 1),
            (SlotId.RelaxZone, 1),
        ]);
    }

    [Fact] // SLOT-02
    public void A_workspace_can_never_hold_more_than_nine_units()
    {
        Rules.TotalCapacity.Should().Be(9);
    }

    [Fact] // SLOT-05
    public void Capacity_is_data_and_every_slot_has_a_rule()
    {
        foreach (var slot in Enum.GetValues<SlotId>())
        {
            var rule = Rules.For(slot);

            rule.Slot.Should().Be(slot);
            // The guard that a rule accepts at least one unit used to be in the SlotRule constructor;
            // it lives in the provider's table now, and this asserts every rule in it behaves.
            rule.MaxQuantity.Should().BeGreaterThan(0);
            rule.DisplayName.Should().NotBeNullOrWhiteSpace();
        }
    }

    [Fact]
    public void Only_the_monitor_slot_accepts_more_than_one_unit()
    {
        Rules.All.Where(rule => rule.MaxQuantity > 1)
            .Select(rule => rule.Slot)
            .Should().Equal(SlotId.Monitor);
    }
}
