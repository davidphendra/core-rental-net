using System.Collections.Immutable;
using CoreRentalNet.BuildingBlocks.Domain;
using CoreRentalNet.Modules.Workspace.Domain;

namespace CoreRentalNet.Modules.Workspace.Application.Rules;

/// <summary>
/// The slot table as data. Changing a capacity here changes behaviour everywhere with no
/// conditional to hunt down.
/// </summary>
/// <remarks>
/// Stateless and immutable, so the table can be shared. It is an injected service rather
/// than a static class because the rules it describes are the application's, and a caller can be
/// given a different table in a test. The capacities come from <see cref="WorkspaceSlotSettings"/>,
/// so a deployment retunes them without a rebuild; what each slot is called and whether it is
/// mandatory stay here.
/// </remarks>
public sealed class SlotRuleProvider : ISlotRuleProvider
{
    private static readonly ImmutableArray<(SlotId Slot, string DisplayName, bool IsMandatory)> s_slots =
    [
        (SlotId.Desk, "Desk", true),
        (SlotId.Chair, "Chair", true),
        (SlotId.Monitor, "Monitor", false),
        (SlotId.Lamp, "Lamp", false),
        (SlotId.Plant, "Plant", false),
        (SlotId.CoffeeStation, "Coffee Station", false),
        (SlotId.RelaxZone, "Relax Zone", false),
    ];

    private readonly ImmutableArray<SlotRule> _rules;

    /// <summary>The shipped table, which is what the defaults describe.</summary>
    public SlotRuleProvider()
        : this(new WorkspaceSlotSettings())
    {
    }

    public SlotRuleProvider(WorkspaceSlotSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        var rules = ImmutableArray.CreateBuilder<SlotRule>(s_slots.Length);

        foreach (var (slot, displayName, isMandatory) in s_slots)
        {
            var capacity = settings.CapacityFor(slot);

            if (capacity < 1)
            {
                throw new DomainRuleViolationException(
                    $"The {displayName} slot must accept at least one unit, but {capacity} was configured.");
            }

            rules.Add(new SlotRule(slot, displayName, capacity, isMandatory));
        }

        _rules = rules.ToImmutable();
    }

    public IReadOnlyList<SlotRule> All => _rules;

    public IReadOnlyList<SlotRule> Mandatory => [.. _rules.Where(rule => rule.IsMandatory)];

    public SlotRule For(SlotId slot)
    {
        foreach (var rule in _rules)
        {
            if (rule.Slot == slot)
            {
                return rule;
            }
        }

        throw new DomainRuleViolationException($"No rule is defined for slot '{slot}'.");
    }

    public int TotalCapacity => _rules.Sum(rule => rule.MaxQuantity);
}
