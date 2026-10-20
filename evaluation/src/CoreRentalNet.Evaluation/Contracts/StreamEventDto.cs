using System.Text.Json.Serialization;

namespace CoreRentalNet.Evaluation.Contracts;

/// <summary>One typed event from the agent's caller-facing stream.</summary>
public sealed record StreamEventDto(
    [property: JsonPropertyName("type")] string Type,
    [property: JsonPropertyName("processingStage")] string? ProcessingStage,
    [property: JsonPropertyName("approvedWorkspaceSetup")] CandidateDto? ApprovedWorkspaceSetup,
    [property: JsonPropertyName("runStatus")] string? RunStatus,
    [property: JsonPropertyName("completedAttemptCount")] int? CompletedAttemptCount);
