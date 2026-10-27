namespace CoreRentalNet.Evaluation.Evaluation;

/// <summary>The stable metric names the report and regression comparer use.</summary>
public static class MetricNames
{
    public const string AnswerExact = "answer_exact";
    public const string ConstraintCompliance = "constraint_compliance";
    public const string RetrievalQuality = "retrieval_quality";
    public const string RawRetrievalRecall = "raw_retrieval_recall";
    public const string ToolCallAccuracy = "tool_call_accuracy";
    public const string WorkflowCorrectness = "workflow_correctness";
    public const string JudgeEvaluated = "judge_evaluated";
    public const string JudgePass = "judge_pass";
    public const string JudgeConfidence = "judge_confidence";
    public const string JudgeAttempts = "judge_attempts";

    public static IReadOnlyList<string> JudgeMetrics { get; } =
    [
        JudgeEvaluated,
        JudgePass,
        JudgeConfidence,
        JudgeAttempts,
    ];

    public static IReadOnlyList<string> All { get; } =
    [
        AnswerExact,
        ConstraintCompliance,
        RetrievalQuality,
        RawRetrievalRecall,
        ToolCallAccuracy,
        WorkflowCorrectness,
    ];

    public static IReadOnlyList<string> CasePassMetrics { get; } = [];
}
