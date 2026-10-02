using System.Text.Json.Serialization;

namespace CoreRentalNet.Agents.Features.WorkspaceSuggestion.Domain;

/// <summary>What one search of one component category came back with, including why it came back with nothing.</summary>
/// <remarks>
/// <para>
/// <b>One entry per component per tool, because a component is searched once for each tool the caller was
/// given.</b> The words written for a name search and the words written for a meaning search are different, so
/// the outcome has to name which search it was: a category's two empty answers are the same absence twice, and
/// its two found answers are not the same products.
/// </para>
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
    [property: JsonPropertyName("tool")]
    [property: JsonRequired]
    string Tool,
    [property: JsonPropertyName("category")]
    [property: JsonRequired]
    WorkspaceComponentCategory ComponentCategory,
    [property: JsonPropertyName("found")]
    [property: JsonRequired]
    int FoundProductCount,
    [property: JsonPropertyName("reason")]
    string? EmptyResultReason);
