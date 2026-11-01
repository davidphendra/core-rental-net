using CoreRentalNet.Evaluation.Evaluation.Pipeline;
using CoreRentalNet.Evaluation.Evaluation.Judging;
using CoreRentalNet.Evaluation.Evaluation.Timing;
using CoreRentalNet.Evaluation.Evaluation.Stochastic;

namespace CoreRentalNet.Evaluation.Evaluation.Checks;

/// <summary>The retrieval result: the selected-set recall and the SKUs that never reached it.</summary>
public sealed record RetrievalResult(
    double SelectedRecall,
    IReadOnlyList<string> FailureCodes);
