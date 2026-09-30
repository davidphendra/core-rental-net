using System.Text.Json.Serialization;

namespace CoreRentalNet.Agents.Features.WorkspaceSuggestion.Domain;

/// <summary>Whether the reviewed setups satisfy the request, and what a next attempt must satisfy when they do not.</summary>
/// <remarks>
/// Validity only. The reviewer never decides what happens next: whether another attempt runs is decided by code,
/// from this verdict and the attempt count.
/// </remarks>
public sealed record WorkspaceSetupReviewResult(
    [property: JsonPropertyName("isAcceptable")]
    bool IsAcceptable,
    [property: JsonPropertyName("issues")]
    IReadOnlyList<WorkspaceSetupReviewIssue> Issues,
    [property: JsonPropertyName("summary")]
    string? Summary);
