using System.Text.Json.Serialization;

namespace CoreRentalNet.Agents.Features.WorkspaceSuggestion.Domain;

/// <summary>One candidate setup: the lines it fills and the composer's one-paragraph rationale.</summary>
/// <remarks>
/// No label and no rank. The application shows the setups in the order they were streamed and gives them no
/// Budget / Balanced / Premium label, so a label can never disagree with a total.
/// </remarks>
public sealed record WorkspaceSetupCandidate(
    [property: JsonPropertyName("lines")]
    IReadOnlyList<WorkspaceSetupLine> Lines,
    [property: JsonPropertyName("rationale")]
    string Rationale);
