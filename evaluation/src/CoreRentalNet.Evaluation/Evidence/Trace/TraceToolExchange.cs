using CoreRentalNet.Evaluation.Evidence.Projection;
using CoreRentalNet.Evaluation.Evidence.Context;

namespace CoreRentalNet.Evaluation.Evidence.Trace;

/// <summary>One tool call and its answer from the raw trace.</summary>
public sealed record TraceToolExchange(
    string Stage,
    int? Attempt,
    string CallId,
    string ToolName,
    IReadOnlyDictionary<string, object?> Arguments,
    string ResultText);
