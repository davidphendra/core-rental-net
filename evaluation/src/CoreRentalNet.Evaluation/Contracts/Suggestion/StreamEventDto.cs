using System.Text.Json.Serialization;

using CoreRentalNet.Evaluation.Contracts.Catalogue;
using CoreRentalNet.Evaluation.Contracts.Reranker;
using CoreRentalNet.Evaluation.Contracts.Trace;
namespace CoreRentalNet.Evaluation.Contracts.Suggestion;

/// <summary>One typed event from the agent's caller-facing stream.</summary>
public sealed record StreamEventDto(
    [property: JsonPropertyName("type")] string Type,
    [property: JsonPropertyName("processingStage")] string? ProcessingStage,
    [property: JsonPropertyName("approvedWorkspaceSetup")] CandidateDto? ApprovedWorkspaceSetup,
    [property: JsonPropertyName("runStatus")] string? RunStatus,
    [property: JsonPropertyName("completedAttemptCount")] int? CompletedAttemptCount);
