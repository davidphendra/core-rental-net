using System.Text.Json.Serialization;

namespace CoreRentalNet.Agents.Shared.Evaluation;

/// <summary>One line in a run's evaluation trace.</summary>
/// <remarks>
/// The run identity, sequence and timestamp are written by the sink, so a record type states only what happened.
/// The discriminator is the wire field a reader switches on, exactly as the caller-facing event stream does.
/// </remarks>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "kind")]
[JsonDerivedType(typeof(EvaluationRunStarted), "runStarted")]
[JsonDerivedType(typeof(EvaluationRunCompleted), "runCompleted")]
[JsonDerivedType(typeof(EvaluationStageModelCall), "stageModelCall")]
[JsonDerivedType(typeof(EvaluationToolExchange), "toolExchange")]
internal abstract record EvaluationTraceRecord(
    string? RunId,
    long Sequence,
    DateTimeOffset At);
