using System.Collections.Immutable;
using CoreRentalNet.BuildingBlocks.Domain;
using CoreRentalNet.Modules.Workspace.Domain;

namespace CoreRentalNet.Modules.Workspace.Application.Workspace.Rules;

/// <summary>
/// The slot table as data. Changing a capacity here changes behaviour everywhere with no
/// conditional to hunt down.
/// </summary>
/// <remarks>
/// Stateless and immutable, so the table can be shared. It is an injected service rather
/// than a static class because the rules it describes are the application's, and a caller can be
/// given a different table in a test.
/// </remarks>
public sealed class SlotRuleProvider : ISlotRuleProvider
{
    private static readonly ImmutableArray<SlotRule> s_rules =
    [
        new(SlotId.Desk, "Desk", 1, IsMandatory: true),
        new(SlotId.Chair, "Chair", 1, IsMandatory: true),
        new(SlotId.Monitor, "Monitor", 3),
        new(SlotId.Lamp, "Lamp", 1),
        new(SlotId.Plant, "Plant", 1),
        new(SlotId.CoffeeStation, "Coffee Station", 1),
        new(SlotId.RelaxZone, "Relax Zone", 1),
    ];

    public IReadOnlyList<SlotRule> All => s_rules;

    public IReadOnlyList<SlotRule> Mandatory => [.. s_rules.Where(rule => rule.IsMandatory)];

    public SlotRule For(SlotId slot)
    {
        foreach (var rule in s_rules)
        {
            if (rule.Slot == slot)
            {
                return rule;
            }
        }

        throw new DomainRuleViolationException($"No rule is defined for slot '{slot}'.");
    }

    public int TotalCapacity => s_rules.Sum(rule => rule.MaxQuantity);
}
