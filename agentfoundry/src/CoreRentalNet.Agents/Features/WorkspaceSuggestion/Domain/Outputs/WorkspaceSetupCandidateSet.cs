using System.Text.Json.Serialization;

namespace CoreRentalNet.Agents.Features.WorkspaceSuggestion.Domain.Outputs;
/// <summary>The composer's answer: the setups it composed, and nothing else.</summary>
/// <remarks>
/// A named envelope rather than a bare array, so a structured-output schema has an object root to hang the
/// property on and an empty answer is still an object the model can produce.
/// </remarks>
public sealed record WorkspaceSetupCandidateSet(
    [property: JsonPropertyName("setups")]
    IReadOnlyList<WorkspaceSetupCandidate> Setups);
