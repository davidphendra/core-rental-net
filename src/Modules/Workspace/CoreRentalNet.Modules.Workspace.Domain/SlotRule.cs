namespace CoreRentalNet.Modules.Workspace.Domain;

/// <summary>What a slot is called, how many units it accepts, and whether it must be filled.</summary>
/// <remarks>
/// A plain record. The table of rules lives in the application's <c>ISlotRuleProvider</c>.
/// </remarks>
public sealed record SlotRule(
    SlotId Slot,
    string DisplayName,
    int MaxQuantity,
    bool IsMandatory = false);
