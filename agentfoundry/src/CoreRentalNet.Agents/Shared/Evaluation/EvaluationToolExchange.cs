namespace CoreRentalNet.Agents.Shared.Evaluation;

/// <summary>One tool call and the answer the model was given for it.</summary>
/// <remarks>
/// The answer is the tool's own text. The external evaluator parses the catalogue contract; the agent's trace
/// deliberately does not.
/// </remarks>
internal sealed record EvaluationToolExchange(
    string Stage,
    int? Attempt,
    string CallId,
    string ToolName,
    IReadOnlyDictionary<string, object?> Arguments,
    string ResultText)
    : EvaluationTraceRecord(null, 0, default);
