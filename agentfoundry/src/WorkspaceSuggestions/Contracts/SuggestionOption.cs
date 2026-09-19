using System.ComponentModel;
using System.Text.Json.Serialization;

namespace WorkspaceSuggestions.Contracts;

/// <summary>One candidate setup: the lines it fills and the model's one-paragraph rationale.</summary>
/// <remarks>
/// No label is carried. The application sorts the candidates by monthly total and labels them
/// Budget / Balanced / Premium by rank, so a label can never disagree with a total.
/// </remarks>
public sealed record SuggestionOption(
    [property: JsonPropertyName("lines")]
    [property: Description("The slot lines this setup fills. A slot the request said nothing about is left out.")]
    IReadOnlyList<SuggestionLine> Lines,
    [property: JsonPropertyName("rationale")]
    [property: Description("A short rationale in purpose words only — no price, no product name, no band label.")]
    string Rationale);
