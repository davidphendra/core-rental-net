using CoreRentalNet.Modules.Workspace.Domain;

namespace CoreRentalNet.Modules.Workspace.Application.Workspace.Commands.ChangeQuantity;

/// <summary>Sets how many of one product a slot holds. Zero removes that product.</summary>
public sealed record ChangeQuantity(string DraftToken, SlotId Slot, string Sku, int Quantity);
