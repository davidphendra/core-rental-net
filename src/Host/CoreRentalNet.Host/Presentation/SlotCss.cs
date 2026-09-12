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
    /// The monitor hangs from the top centre; the lamp stands at the left of the desk and the plant at
    /// the right, both a little above it; the desk spans the width with the chair in front of it, half
    /// below the stage's inner box. Which side the lamp stands on, and how far the chair hangs, are
    /// deliberate differences from the design, at the owner's request: the design puts both the lamp
    /// and the plant on the right, a hand's width apart.
    /// </remarks>
    public static string StagePositionFor(SlotId slot) => slot switch
    {
        SlotId.Monitor => "absolute top-0 left-1/2 -translate-x-1/2",
        SlotId.Lamp => "absolute bottom-12 left-12",
        SlotId.Plant => "absolute bottom-12 right-12",
        SlotId.Desk => "relative z-0",
        // The chair used to sit tucked under the desk, 64px below it and 160px tall - tall enough to
        // cover the desk's own 32px bar, which is the one place a desk can be clicked from. So the
        // desk could not be added at all. It now stops at the desk's baseline, one chair-height
        // below, and is the lamp's size, so neither box ever covers the other. The design draws the
        // chair tucked in; this is a deliberate difference from it.
        SlotId.Chair => "absolute -bottom-28 left-1/2 -translate-x-1/2 z-20",
        _ => throw new ArgumentOutOfRangeException(nameof(slot), slot, "A zone is laid out by the grid, not positioned."),
    };

    /// <summary>How big a box holding this slot is drawn.</summary>
    /// <remarks>
    /// A monitor is drawn wide and shallow, and three of them stand side by side in the width the
    /// design gives one. They are smaller than the design's box, at the owner's request, which is also
    /// what leaves room between the row and the desk below it.
    /// </remarks>
    public static string BoxSizeFor(SlotId slot) => slot switch
    {
        SlotId.Monitor => "w-40 h-28",
        SlotId.Lamp or SlotId.Plant or SlotId.Chair => "w-24 h-24",
        SlotId.Desk => "w-full h-8",
        _ => throw new ArgumentOutOfRangeException(nameof(slot), slot, "A zone is sized by the grid, not by a box."),
    };
}
