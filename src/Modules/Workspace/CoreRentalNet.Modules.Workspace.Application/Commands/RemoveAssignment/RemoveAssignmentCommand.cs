using CoreRentalNet.Modules.Workspace.Domain;

namespace CoreRentalNet.Modules.Workspace.Application.Commands.RemoveAssignment;

/// <summary>Empty a slot. Removing an empty slot is a no-op, so a double click is harmless.</summary>
public sealed record RemoveAssignmentCommand(string DraftToken, SlotId Slot);
