namespace CoreRentalNet.Agents.Shared.Evaluation;

/// <summary>One stage's model call and the raw answer it produced.</summary>
/// <remarks>
/// The answer is retained verbatim because the external evaluator reads the reranker's JSON. Nothing in the agent
/// interprets it here; this is evidence, not a decision.
/// </remarks>
internal sealed record EvaluationStageModelCall(
    string Stage,
    int? Attempt,
    string ResponseText,
    IReadOnlyList<EvaluationToolCall> ToolCalls,
    int InputTokens,
    int OutputTokens,
    string? Phase = null)
    : EvaluationTraceRecord(null, 0, default);
