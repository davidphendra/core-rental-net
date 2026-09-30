using System.Text.Json.Serialization;

namespace CoreRentalNet.Agents.Features.WorkspaceSuggestion.Domain;

/// <summary>The most the whole workspace may cost each month, and whether a customer stated it.</summary>
/// <remarks>
/// A type of its own rather than one shape shared with the per-component budget: the wire names differ, and a
/// total is never derived, so a derived flag here could only ever be false.
/// </remarks>
public sealed record WorkspaceTotalMonthlyBudget(
    [property: JsonPropertyName("amount")]
    [property: JsonRequired]
    int? Amount,
    [property: JsonPropertyName("currency")]
    [property: JsonRequired]
    string? Currency,
    [property: JsonPropertyName("is_explicit")]
    [property: JsonRequired]
    bool IsExplicit);
