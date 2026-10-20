namespace CoreRentalNet.Evaluation.Evaluation;

/// <summary>The retrieval result: the selected-set recall and the SKUs that never reached it.</summary>
public sealed record RetrievalResult(
    double SelectedRecall,
    IReadOnlyList<string> FailureCodes);
