using CoreRentalNet.Evaluation.Metrics.Retrieval;
using CoreRentalNet.Evaluation.Metrics.Statistics;
using CoreRentalNet.Evaluation.Metrics.Rules;
using CoreRentalNet.Evaluation.Metrics.Thresholds;

namespace CoreRentalNet.Evaluation.Metrics.Outcome;

/// <summary>One class's precision, recall and F1, with the support they were measured over.</summary>
public sealed record OutcomeClassScore(
    string Outcome,
    int Support,
    double Precision,
    double Recall,
    double F1);
