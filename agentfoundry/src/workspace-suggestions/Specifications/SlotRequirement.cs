using AgentFoundry.WorkspaceSuggestions.Contracts;

namespace AgentFoundry.WorkspaceSuggestions.Specifications;

/// <summary>One slot the workspace will have, how many units it holds, and where that came from.</summary>
/// <remarks>
/// <paramref name="Inferred"/> is the part worth keeping: it says whether the slot came from the
/// declared table or from the model, so a reviewer can check an inferred slot harder than a stated one
/// and the run record can report how often the table missed.
/// </remarks>
public sealed record SlotRequirement(string Slot, int Quantity, bool Inferred);
