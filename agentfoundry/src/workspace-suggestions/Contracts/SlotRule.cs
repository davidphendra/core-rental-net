namespace AgentFoundry.WorkspaceSuggestions.Contracts;

/// <summary>One slot as the application holds it: what it is called, how many units, and whether it is required.</summary>
/// <remarks>
/// The slot is a string rather than an enum here on purpose: the schema closes the vocabulary, and a
/// type in this project that mirrored it would be a second list to keep in step. What arrives is
/// validated against the schema's list.
/// </remarks>
public sealed record SlotRule(
    string Slot,
    string DisplayName,
    int MaxQuantity,
    bool IsMandatory);
