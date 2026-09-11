using CoreRentalNet.Modules.Workspace.Domain;

namespace CoreRentalNet.Modules.Workspace.Application.Workspace;

public sealed record WorkspaceView(
    Guid WorkspaceId,
    DraftState State,
    int Version,
    IReadOnlyList<AssignableSlot> Slots,
    string? DeliveryAddress,
    int TotalUnits,
    bool IsEmpty,
    WorkspaceQuote Quote,
    IReadOnlyList<string> MissingSlotNames)
{
    /// <summary>
    /// Whether the workspace can be turned into an order: something in it, a desk and a chair among
    /// what is in it, and nothing that has left the catalog.
    /// </summary>
    public bool CanCheckout => !IsEmpty && MissingSlotNames.Count == 0 && !Quote.HasUnavailableLines;

    /// <summary>
    /// Why this workspace cannot be rented yet, or null when it can.
    /// </summary>
    /// <remarks>
    /// One text, in one place, so every control that shuts the way out says the same thing about the
    /// same workspace rather than each inventing its own wording.
    /// </remarks>
    public string? BlockingReason
    {
        get
        {
            if (Quote.HasUnavailableLines)
            {
                return "An item in your workspace is no longer in the catalog. Remove it before renting.";
            }

            if (MissingSlotNames.Count > 0)
            {
                var names = string.Join(" and a ", MissingSlotNames.Select(name => name.ToLowerInvariant()));
                var verb = MissingSlotNames.Count == 1 ? "is" : "are";

                return $"A {names} {verb} required before you can rent.";
            }

            return IsEmpty ? "Add an item to your workspace first." : null;
        }
    }
}
