namespace CoreRentalNet.Evaluation.Evaluation;

/// <summary>One judge outcome for one evaluation item.</summary>
public sealed record JudgeEvaluation(
    bool Evaluated,
    bool Passed,
    double Confidence,
    string Reason)
{
    /// <summary>The number of attempts made for this judge decision.</summary>
    public int Attempts { get; init; } = 1;

    public static JudgeEvaluation NotEvaluated { get; } = new(false, false, 0, string.Empty)
    {
        Attempts = 0,
    };

    public static JudgeEvaluation Error(string reason)
        => new(true, false, 0, $"JUDGE_ERROR: {reason}");
}
