namespace CoreRentalNet.Evaluation.Evidence;

/// <summary>One stage model call from the raw trace.</summary>
public sealed record TraceStageCall(
    string Stage,
    int? Attempt,
    string ResponseText);
