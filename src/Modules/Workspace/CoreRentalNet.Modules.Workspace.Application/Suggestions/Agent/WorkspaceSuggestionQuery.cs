namespace CoreRentalNet.Modules.Workspace.Application.Suggestions.Agent;

/// <summary>What a caller asks a suggestion run for: the sentence, and a ceiling if they gave one.</summary>
/// <remarks>
/// Deliberately the whole of the use case's input. The browser does not send a draft, a slot, a SKU or a
/// price: the catalogue and the slot rules are the application's, and a caller that could name them could name
/// a product the catalogue does not hold.
/// </remarks>
public sealed record WorkspaceSuggestionQuery(string Query, int? CeilingMonthly);
