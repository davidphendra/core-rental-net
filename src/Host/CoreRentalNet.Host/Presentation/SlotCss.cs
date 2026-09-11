using CoreRentalNet.Modules.Workspace.Domain;

namespace CoreRentalNet.Host.Presentation;

/// <summary>
/// Maps a slot to the CSS class that positions it.
/// </summary>
/// <remarks>
/// This is the only place the UI translates a slot into geometry, and it translates it into a
/// class name, not into coordinates. The classes are the design's own, copied from its markup,
/// which places the seven positions absolutely instead of laying them out (ADR-0008).
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

    /// <summary>Where a box for this slot sits on the stage, in the design's own classes.</summary>
    /// <remarks>
    /// The monitor hangs from the top centre; the lamp and the plant stand at the right; the desk
    /// spans the width with the chair in front of it, half below the stage's inner box.
    /// </remarks>
    public static string StagePositionFor(SlotId slot) => slot switch
    {
        SlotId.Monitor => "absolute top-0 left-1/2 -translate-x-1/2",
        SlotId.Lamp => "absolute bottom-8 right-40",
        SlotId.Plant => "absolute bottom-8 right-12",
        SlotId.Desk => "relative z-0",
        SlotId.Chair => "absolute -bottom-16 left-1/2 -translate-x-1/2 z-20",
        _ => throw new ArgumentOutOfRangeException(nameof(slot), slot, "A zone is laid out by the grid, not positioned."),
    };

    /// <summary>How big the design draws a box holding this slot.</summary>
    /// <remarks>
    /// A monitor is drawn wide and shallow, and three of them stand side by side in the width the
    /// design gives one, which is what makes a row of three fit inside the scene.
    /// </remarks>
    public static string BoxSizeFor(SlotId slot) => slot switch
    {
        SlotId.Monitor => "w-48 h-32",
        SlotId.Lamp or SlotId.Plant => "w-24 h-24",
        SlotId.Desk => "w-full h-8",
        SlotId.Chair => "w-32 h-40",
        _ => throw new ArgumentOutOfRangeException(nameof(slot), slot, "A zone is sized by the grid, not by a box."),
    };
}
