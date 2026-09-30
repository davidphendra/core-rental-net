using System.Text.Json.Serialization;

namespace CoreRentalNet.Modules.Workspace.Infrastructure.Suggestions;

/// <summary>One event of the agent's streamed answer, as the agent's schema spells it.</summary>
/// <remarks>
/// <para>
/// The agent no longer answers with one document: it streams typed events — a stage beginning or ending, an
/// approved setup, a retry beginning, and the run's ending. <c>type</c> is how a reader tells them apart, and the
/// properties that matter are all optional because only some events carry them.
/// </para>
/// <para>
/// The provider's wire shape stops at the reader: nothing downstream of this type names a streamed event.
/// </para>
/// </remarks>
internal sealed record MicrosoftFoundrySuggestionStreamEvent(
    [property: JsonPropertyName("type")] string? Type,
    [property: JsonPropertyName("approvedWorkspaceSetup")] MicrosoftFoundrySuggestionAgentOption? ApprovedWorkspaceSetup,
    [property: JsonPropertyName("runStatus")] string? RunStatus,
    [property: JsonPropertyName("outcomeReason")] string? OutcomeReason,
    [property: JsonPropertyName("completedAttemptCount")] int CompletedAttemptCount,
    [property: JsonPropertyName("runUsage")] MicrosoftFoundrySuggestionAgentRunUsage? RunUsage,
    [property: JsonPropertyName("processingStage")] string? ProcessingStage,
    [property: JsonPropertyName("nextAttemptNumber")] int NextAttemptNumber,
    [property: JsonPropertyName("maximumAttemptCount")] int MaximumAttemptCount);
