using System.Text.Json.Serialization;

namespace CoreRentalNet.Agents.Features.WorkspaceSuggestion.Domain;

/// <summary>What searching one component category came back with, including why it came back with nothing.</summary>
/// <remarks>
/// <para>
/// <b>An empty answer the budget caused and an empty answer the catalogue caused are the same absence and
/// different problems, and only the first is fixable.</b> Without this, a run whose derived allocation could not
/// buy a desk reports that no workspace exists, and the retry that is meant to correct the allocation is told
/// nothing to correct.
/// </para>
/// <para>
/// The reason is the tool's own words rather than a summary of them, because it is the only thing that carries
/// the number — the cheapest product the ceiling excluded.
/// </para>
/// </remarks>
public sealed record WorkspaceComponentSearchOutcome(
    [property: JsonPropertyName("category")]
    [property: JsonRequired]
    WorkspaceComponentCategory ComponentCategory,
    [property: JsonPropertyName("found")]
    [property: JsonRequired]
    int FoundProductCount,
    [property: JsonPropertyName("reason")]
    string? EmptyResultReason);
