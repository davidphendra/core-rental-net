namespace CoreRentalNet.Modules.Workspace.Application.Workspace.Commands.AssignProduct;

/// <summary>
/// Adds a product to the draft. The customer supplies a SKU, never a slot: the slot comes
/// from what the product is.
/// </summary>
public sealed record AssignProduct(string DraftToken, string Sku, int Quantity = 1);
