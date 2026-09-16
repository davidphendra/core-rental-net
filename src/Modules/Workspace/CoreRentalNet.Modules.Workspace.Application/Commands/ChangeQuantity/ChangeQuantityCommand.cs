using CoreRentalNet.Modules.Workspace.Domain;

namespace CoreRentalNet.Modules.Workspace.Application.Commands.ChangeQuantity;

/// <summary>Sets how many of one product a slot holds. Zero removes that product.</summary>
public sealed record ChangeQuantityCommand(string DraftToken, SlotId Slot, string Sku, int Quantity);
