namespace CoreRentalNet.Evaluation.Evidence;

/// <summary>One catalogue answer, attributed to the slot it was searched for.</summary>
public sealed record ToolAnswerEvidence(
    string Slot,
    string ToolName,
    IReadOnlyDictionary<string, object?> Arguments,
    IReadOnlyList<string> ReturnedSkus,
    int? Attempt,
    string ResultText);
