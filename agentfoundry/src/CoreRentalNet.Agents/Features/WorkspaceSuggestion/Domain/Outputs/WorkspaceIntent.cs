using System.Text.Json.Serialization;

namespace CoreRentalNet.Agents.Features.WorkspaceSuggestion.Domain.Outputs;
/// <summary>What the whole workspace is for, across every component.</summary>
/// <remarks>
/// Phrases rather than products, and never a search query: it exists so the reviewer can hold a composed set
/// against what the workspace was meant to be, which no single component's words can answer. The four lists are
/// separate because they are separate questions, and a flat list of them would leave the reviewer to guess which
/// one a phrase answered.
/// </remarks>
public sealed record WorkspaceIntent(
    [property: JsonPropertyName("purpose")]
    [property: JsonRequired]
    IReadOnlyList<string> PurposePhrases,
    [property: JsonPropertyName("style")]
    [property: JsonRequired]
    IReadOnlyList<string> StylePhrases,
    [property: JsonPropertyName("experience")]
    [property: JsonRequired]
    IReadOnlyList<string> ExperiencePhrases,
    [property: JsonPropertyName("usage")]
    [property: JsonRequired]
    IReadOnlyList<string> UsagePhrases);
