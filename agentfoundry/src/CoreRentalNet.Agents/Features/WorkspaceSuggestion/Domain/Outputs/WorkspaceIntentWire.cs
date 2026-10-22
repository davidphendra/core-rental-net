using System.Text.Json.Serialization;

namespace CoreRentalNet.Agents.Features.WorkspaceSuggestion.Domain.Outputs;

/// <summary>The rephraser's compact intent: only the phrases it actually found.</summary>
/// <remarks>
/// The model-facing twin of <see cref="WorkspaceIntent"/>. A list the model has nothing to say about is omitted,
/// which keeps three empty arrays out of every answer; the mapper turns an omitted list back into the empty list
/// the domain type requires.
/// </remarks>
internal sealed record WorkspaceIntentWire(
    [property: JsonPropertyName("purpose")]
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    IReadOnlyList<string>? PurposePhrases = null,
    [property: JsonPropertyName("style")]
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    IReadOnlyList<string>? StylePhrases = null,
    [property: JsonPropertyName("experience")]
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    IReadOnlyList<string>? ExperiencePhrases = null,
    [property: JsonPropertyName("usage")]
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    IReadOnlyList<string>? UsagePhrases = null);
