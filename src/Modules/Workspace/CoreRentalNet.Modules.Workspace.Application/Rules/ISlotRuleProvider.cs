using CoreRentalNet.Modules.Workspace.Domain;

namespace CoreRentalNet.Modules.Workspace.Application.Rules;

/// <summary>The slot table: what each slot accepts and which slots are mandatory.</summary>
public interface ISlotRuleProvider
{
    IReadOnlyList<SlotRule> All { get; }

    /// <summary>The slots a workspace must hold something in before it can be rented.</summary>
    IReadOnlyList<SlotRule> Mandatory { get; }

    SlotRule For(SlotId slot);

    /// <summary>The most units a workspace can ever hold: the sum of every slot's capacity.</summary>
    int TotalCapacity { get; }
}
