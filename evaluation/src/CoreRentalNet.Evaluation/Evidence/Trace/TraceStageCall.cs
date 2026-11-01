using CoreRentalNet.Evaluation.Evidence.Projection;
using CoreRentalNet.Evaluation.Evidence.Context;

namespace CoreRentalNet.Evaluation.Evidence.Trace;

/// <summary>One stage model call from the raw trace.</summary>
public sealed record TraceStageCall(
    string Stage,
    int? Attempt,
    string ResponseText);
