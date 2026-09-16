using CoreRentalNet.Modules.Workspace.Domain;

namespace CoreRentalNet.Modules.Workspace.Application.Rules;

/// <summary>
/// How many units each slot accepts: the thresholds that decide what a workspace can hold.
/// </summary>
/// <remarks>
/// The defaults are the shipped table, so an environment that configures nothing behaves exactly as
/// before. The composition root reads the values; the application never sees configuration, and the
/// two slots that are mandatory stay the application's, because that is structure rather than a
/// number.
/// </remarks>
public sealed record WorkspaceSlotSettings(
    int Desk = 1,
    int Chair = 1,
    int Monitor = 3,
    int Lamp = 1,
    int Plant = 1,
    int CoffeeStation = 1,
    int RelaxZone = 1)
{
    /// <summary>The number of units the given slot accepts, as configured.</summary>
    public int CapacityFor(SlotId slot) => slot switch
    {
        SlotId.Desk => Desk,
        SlotId.Chair => Chair,
        SlotId.Monitor => Monitor,
        SlotId.Lamp => Lamp,
        SlotId.Plant => Plant,
        SlotId.CoffeeStation => CoffeeStation,
        SlotId.RelaxZone => RelaxZone,
        _ => throw new ArgumentOutOfRangeException(nameof(slot), slot, "No capacity is configured for this slot."),
    };
}
