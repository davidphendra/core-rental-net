using System.Text.Json.Serialization;

namespace CoreRentalNet.Agents.Features.WorkspaceSuggestion.Domain;

/// <summary>The reranker's whole answer: every component's products, judged independently.</summary>
public sealed record WorkspaceComponentProductRankingResult(
    [property: JsonPropertyName("categories")]
    [property: JsonRequired]
    WorkspaceComponentProductAssessments Assessments);
