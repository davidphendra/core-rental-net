using System.Text.Json.Serialization;

namespace CoreRentalNet.Agents.Features.WorkspaceSuggestion.Domain.Outputs;
/// <summary>Advisory observations about a composed setup.</summary>
/// <remarks>The reranker result is authoritative. This object is a report and must not route or reject.</remarks>
public sealed record WorkspaceSetupReviewResult(
    [property: JsonPropertyName("isAcceptable")]
    bool IsAcceptable,
    [property: JsonPropertyName("issues")]
    IReadOnlyList<WorkspaceSetupReviewIssue> Issues,
    [property: JsonPropertyName("summary")]
    string? Summary);
