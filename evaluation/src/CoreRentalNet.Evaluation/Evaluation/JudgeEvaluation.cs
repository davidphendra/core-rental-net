namespace CoreRentalNet.Evaluation.Evaluation;

/// <summary>One judge outcome for one evaluation item.</summary>
public sealed record JudgeEvaluation(
    bool Evaluated,
    bool Passed,
    double Confidence,
    string Reason)
{
    public static JudgeEvaluation NotEvaluated { get; } = new(false, false, 0, string.Empty);

    public static JudgeEvaluation Error(string reason)
        => new(true, false, 0, $"JUDGE_ERROR: {reason}");
}
