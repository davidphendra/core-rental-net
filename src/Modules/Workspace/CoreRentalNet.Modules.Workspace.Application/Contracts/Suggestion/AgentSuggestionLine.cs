namespace CoreRentalNet.Modules.Workspace.Application.Contracts.Suggestion;

/// <summary>One product the agent chose, as it states it: a slot, a SKU and how many.</summary>
/// <remarks>
/// No price and no name. The agent never states an amount, and this is where that rule is visible:
/// there is no field for one to arrive in.
/// </remarks>
public sealed record AgentSuggestionLine(string Slot, string Sku, int Quantity);
