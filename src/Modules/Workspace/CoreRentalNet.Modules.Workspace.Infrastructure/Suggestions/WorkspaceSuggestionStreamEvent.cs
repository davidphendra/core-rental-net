using System.Text.Json.Serialization;

namespace CoreRentalNet.Modules.Workspace.Infrastructure.Suggestions;

/// <summary>One event of the agent's streamed answer, as the agent states it.</summary>
/// <remarks>
/// <para>
/// <b>A projection, not the whole contract.</b> Only what a reader uses is mapped: the discriminator is the enum
/// rather than a string, and a field the application never reads — an outcome reason, an attempt count — is not
/// mapped at all. The serializer ignores the rest of the wire, exactly as it already ignores
/// <c>customerWorkflowIdentifier</c>. The whole text is still kept as evidence on the run record.
/// </para>
/// <para>
/// The provider's wire shape stops at the reader: nothing downstream of this type names a streamed event.
/// </para>
/// </remarks>
internal sealed record WorkspaceSuggestionStreamEvent(
    [property: JsonPropertyName("type")] WorkspaceSuggestionStreamEventType? Type,
    [property: JsonPropertyName("processingStage")] string? ProcessingStage,
    [property: JsonPropertyName("approvedWorkspaceSetup")] WorkspaceSuggestionAgentOption? ApprovedWorkspaceSetup,
    [property: JsonPropertyName("nextAttemptNumber")] int NextAttemptNumber,
    [property: JsonPropertyName("maximumAttemptCount")] int MaximumAttemptCount,
    [property: JsonPropertyName("runStatus")] string? RunStatus,
    [property: JsonPropertyName("runUsage")] WorkspaceSuggestionAgentRunUsage? RunUsage);
