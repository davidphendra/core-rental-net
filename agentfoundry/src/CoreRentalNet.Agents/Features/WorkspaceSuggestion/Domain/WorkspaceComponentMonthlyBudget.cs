using System.Text.Json.Serialization;

namespace CoreRentalNet.Agents.Features.WorkspaceSuggestion.Domain;

/// <summary>The most one component may cost each month, and whether the customer stated it or the model derived
/// it.</summary>
/// <remarks>
/// <b>The two flags are not the same fact, which is why both are here.</b> A stated amount is the customer's; a
/// derived amount is this stage's division of a total, and a later reader has to be able to tell which one it is
/// looking at before it decides what an overrun means.
/// </remarks>
public sealed record WorkspaceComponentMonthlyBudget(
    [property: JsonPropertyName("max_amount")]
    [property: JsonRequired]
    int? MaximumAmount,
    [property: JsonPropertyName("currency")]
    [property: JsonRequired]
    string? Currency,
    [property: JsonPropertyName("is_explicit")]
    [property: JsonRequired]
    bool IsExplicit,
    [property: JsonPropertyName("is_derived")]
    [property: JsonRequired]
    bool IsDerived);
