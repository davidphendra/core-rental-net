using System.Text.Json.Serialization;

namespace CoreRentalNet.Evaluation.Contracts;

/// <summary>One line of the raw evaluation trace. The unused members are null for a given kind.</summary>
public sealed record EvaluationTraceRecordDto(
    [property: JsonPropertyName("kind")] string Kind,
    [property: JsonPropertyName("runId")] string? RunId,
    [property: JsonPropertyName("stage")] string? Stage,
    [property: JsonPropertyName("attempt")] int? Attempt,
    [property: JsonPropertyName("responseText")] string? ResponseText,
    [property: JsonPropertyName("callId")] string? CallId,
    [property: JsonPropertyName("toolName")] string? ToolName,
    [property: JsonPropertyName("arguments")] IReadOnlyDictionary<string, object?>? Arguments,
    [property: JsonPropertyName("resultText")] string? ResultText);
