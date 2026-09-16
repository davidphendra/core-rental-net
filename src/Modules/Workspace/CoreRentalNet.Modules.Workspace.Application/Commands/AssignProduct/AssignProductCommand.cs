namespace CoreRentalNet.Modules.Workspace.Application.Commands.AssignProduct;

/// <summary>
/// Adds a product to the draft. The customer supplies a SKU, never a slot: the slot comes
/// from what the product is.
/// </summary>
public sealed record AssignProductCommand(string DraftToken, string Sku, int Quantity = 1);
