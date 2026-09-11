using CoreRentalNet.BuildingBlocks.Domain;

namespace CoreRentalNet.Modules.Workspace.Domain;

/// <summary>What a slot is called, how many units it accepts, and whether it must be filled.</summary>
public sealed record SlotRule
{
    public SlotRule(SlotId slot, string displayName, int maxQuantity, bool isMandatory = false)
    {
        if (maxQuantity < 1)
        {
            throw new DomainRuleViolationException($"Slot '{slot}' must accept at least one unit.");
        }

        Slot = slot;
        DisplayName = Guard.NotEmpty(displayName, "Slot display name", 40);
        MaxQuantity = maxQuantity;
        IsMandatory = isMandatory;
    }

    public SlotId Slot { get; }

    public string DisplayName { get; }

    public int MaxQuantity { get; }

    /// <summary>A workspace cannot be rented without one of these.</summary>
    public bool IsMandatory { get; }
}

/// <summary>
/// The slot table as data. Changing a capacity here changes behaviour everywhere with no
/// conditional to hunt down (ADR-0008).
/// </summary>
public static class SlotRules
{
    private static readonly SlotRule[] Rules =
    [
        new(SlotId.Desk, "Desk", 1, isMandatory: true),
        new(SlotId.Chair, "Chair", 1, isMandatory: true),
        new(SlotId.Monitor, "Monitor", 3),
        new(SlotId.Lamp, "Lamp", 1),
        new(SlotId.Plant, "Plant", 1),
        new(SlotId.CoffeeStation, "Coffee Station", 1),
        new(SlotId.RelaxZone, "Relax Zone", 1),
    ];

    public static IReadOnlyList<SlotRule> All => Rules;

    /// <summary>The slots a workspace must hold something in before it can be rented.</summary>
    public static IReadOnlyList<SlotRule> Mandatory => [.. Rules.Where(rule => rule.IsMandatory)];

    public static SlotRule For(SlotId slot)
    {
        foreach (var rule in Rules)
        {
            if (rule.Slot == slot)
            {
                return rule;
            }
        }

        throw new DomainRuleViolationException($"No rule is defined for slot '{slot}'.");
    }

    /// <summary>The most units a workspace can ever hold: the sum of every slot's capacity.</summary>
    public static int TotalCapacity => Rules.Sum(rule => rule.MaxQuantity);
}
