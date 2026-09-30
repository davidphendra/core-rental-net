using System.Text.Json.Serialization;

namespace CoreRentalNet.Agents.Features.WorkspaceSuggestion.Domain.StreamingEvents;

/// <summary>One thing a run tells its caller while it happens.</summary>
/// <remarks>
/// <para>
/// The application's vocabulary, not the model's, and a closed hierarchy: a new event is a new type, so a
/// consumer that switches over these cannot silently drop one. Nothing here is derived from raw model text —
/// a stage agent's words never reach a caller, only the events a stage chooses to publish.
/// </para>
/// <para>
/// The discriminator is the wire field a caller switches on, because the stream is the caller's contract and the
/// C# type names are the application's own.
/// </para>
/// </remarks>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(WorkspaceProcessingStageStartedEvent), "stageStarted")]
[JsonDerivedType(typeof(WorkspaceProcessingStageCompletedEvent), "stageCompleted")]
[JsonDerivedType(typeof(WorkspaceSetupCandidateApprovedEvent), "candidate")]
[JsonDerivedType(typeof(WorkspaceSetupRetryStartedEvent), "retry")]
[JsonDerivedType(typeof(WorkspaceSuggestionRunCompletedEvent), "completed")]
public abstract record WorkspaceSuggestionStreamEvent(string CustomerWorkflowIdentifier);
