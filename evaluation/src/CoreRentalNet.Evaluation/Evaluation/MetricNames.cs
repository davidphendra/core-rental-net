namespace CoreRentalNet.Evaluation.Evaluation;

/// <summary>The stable metric names the report and regression comparer use.</summary>
public static class MetricNames
{
    public const string AnswerExact = "answer_exact";
    public const string ConstraintCompliance = "constraint_compliance";
    public const string RetrievalQuality = "retrieval_quality";
    public const string ToolCallAccuracy = "tool_call_accuracy";
    public const string WorkflowCorrectness = "workflow_correctness";

    public static IReadOnlyList<string> All { get; } =
    [
        AnswerExact,
        ConstraintCompliance,
        RetrievalQuality,
        ToolCallAccuracy,
        WorkflowCorrectness,
    ];
}
