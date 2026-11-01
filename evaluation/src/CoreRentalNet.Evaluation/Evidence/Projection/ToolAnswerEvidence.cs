using CoreRentalNet.Evaluation.Evidence.Trace;
using CoreRentalNet.Evaluation.Evidence.Context;

namespace CoreRentalNet.Evaluation.Evidence.Projection;

/// <summary>One catalogue answer, attributed to the slot it was searched for.</summary>
public sealed record ToolAnswerEvidence(
    string Slot,
    string ToolName,
    IReadOnlyDictionary<string, object?> Arguments,
    IReadOnlyList<string> ReturnedSkus,
    int? Attempt,
    string ResultText);
