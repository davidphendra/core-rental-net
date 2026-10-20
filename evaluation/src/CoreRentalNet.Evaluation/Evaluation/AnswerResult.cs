namespace CoreRentalNet.Evaluation.Evaluation;

/// <summary>The answer-correctness result: the exact gate plus the near-miss score.</summary>
public sealed record AnswerResult(
    bool Exact,
    double F1,
    IReadOnlyList<string> FailureCodes);
