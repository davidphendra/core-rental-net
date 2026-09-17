namespace CoreRentalNet.Modules.Workspace.Application.Commands.ApplyComposition;

/// <summary>One line of a composition to apply: which slot, which product, how many.</summary>
/// <remarks>
/// The slot is here and is not on the read model's line, which is the whole difference between the two:
/// reading a workspace can work out where a product sits from the catalogue, and writing one has to be
/// told. It is a plain string rather than a <c>SlotId</c> so that a caller holding a composition from
/// outside the module - the page, holding a candidate the agent composed - is not required to know the
/// module's enum to hand it over.
/// </remarks>
public sealed record CompositionLine(string Slot, string Sku, int Quantity);
