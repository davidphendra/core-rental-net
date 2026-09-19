using CoreRentalNet.Modules.Workspace.Domain;

namespace CoreRentalNet.Modules.Workspace.Application.Commands.ReplaceComposition;

/// <summary>One line of a composition a caller wants the workspace replaced with.</summary>
/// <remarks>
/// A slot, a SKU and a quantity - the whole of what a composition is. It carries no price and no name,
/// because neither belongs to a draft: prices are recomputed from the catalogue on every read and frozen only
/// when an order is issued.
/// </remarks>
public sealed record ReplaceCompositionLine(SlotId Slot, string Sku, int Quantity);
