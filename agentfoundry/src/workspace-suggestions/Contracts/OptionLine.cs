namespace AgentFoundry.WorkspaceSuggestions.Contracts;

/// <summary>One product filling one slot, as an option carries it: a SKU and how many of it.</summary>
/// <remarks>
/// No price, no name and no image. The application resolves those from its own catalogue and recomputes
/// every amount, so a language model can never influence what a customer is charged.
/// </remarks>
public sealed record OptionLine(string Slot, string Sku, int Quantity);
