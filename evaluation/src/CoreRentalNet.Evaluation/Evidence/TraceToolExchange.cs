namespace CoreRentalNet.Evaluation.Evidence;

/// <summary>One tool call and its answer from the raw trace.</summary>
public sealed record TraceToolExchange(
    string Stage,
    int? Attempt,
    string CallId,
    string ToolName,
    IReadOnlyDictionary<string, object?> Arguments,
    string ResultText);
