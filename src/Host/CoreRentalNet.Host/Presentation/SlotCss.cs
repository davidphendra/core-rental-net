using CoreRentalNet.Modules.Workspace.Domain;

namespace CoreRentalNet.Host.Presentation;

/// <summary>
/// Maps a slot to the CSS class that positions it.
/// </summary>
/// <remarks>
/// This is the only place the UI translates a slot into geometry, and it translates it into a
/// class name, not into coordinates. The coordinates live in <c>slots.css</c> (ADR-0008).
/// </remarks>
public static class SlotCss
{
    public static string ClassFor(SlotId slot) => slot switch
    {
        SlotId.Desk => "slot--desk",
        SlotId.Chair => "slot--chair",
        SlotId.Monitor => "slot--monitor",
        SlotId.Lamp => "slot--lamp",
        SlotId.Plant => "slot--plant",
        SlotId.CoffeeStation => "slot--coffee-station",
        SlotId.RelaxZone => "slot--relax-zone",
        _ => throw new ArgumentOutOfRangeException(nameof(slot), slot, "No CSS class is defined for this slot."),
    };
}
