using CoreRentalNet.BuildingBlocks.Domain;

namespace CoreRentalNet.Modules.Workspace.Application.Contracts.Suggestion;

/// <summary>One product in a candidate, with its name and price taken from the catalogue.</summary>
/// <remarks>
/// The name and the price are the application's, resolved from its own catalogue at the moment it
/// answers. The agent states a SKU and the application decides what that SKU costs - which is the rule
/// that means a language model can never influence what a customer is charged.
/// </remarks>
public sealed record SuggestedLine(string Slot, string Sku, string Name, int Quantity, Money UnitMonthlyPrice);
