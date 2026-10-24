namespace CoreRentalNet.Evaluation.Evaluation;

/// <summary>The before/after reranker differences supplied to the judge.</summary>
public sealed record RerankerMismatch(
    IReadOnlyList<string> ExpectedMissingFromRaw,
    IReadOnlyList<string> ExpectedDroppedByReranker,
    IReadOnlyList<string> SelectedNotExpected,
    bool ComposerChangedSelection,
    string Summary);
