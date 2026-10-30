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
    public const string LlmFaithfulness = "llm_faithfulness";
    public const string LlmAnswerCorrectness = "llm_answer_correctness";
    public const string LlmContextPrecision = "llm_context_precision";
    public const string LlmContextRecall = "llm_context_recall";
    public const string AgentEvalToolChain = "agenteval_tool_chain";
    public const string AgentEvalWorkflowNodes = "agenteval_workflow_nodes";

    public static IReadOnlyList<string> JudgeMetrics { get; } =
    [
        JudgeEvaluated,
        JudgePass,
        JudgeConfidence,
        JudgeAttempts,
    ];

    /// <summary>The AgentEval RAG metrics. They are diagnostics until a calibration baseline promotes them.</summary>
    public static IReadOnlyList<string> RagMetrics { get; } =
    [
        LlmFaithfulness,
        LlmAnswerCorrectness,
        LlmContextPrecision,
        LlmContextRecall,
    ];

    public static IReadOnlyList<string> All { get; } =
    [
        AnswerExact,
        ConstraintCompliance,
        RetrievalQuality,
        RawRetrievalRecall,
        ToolCallAccuracy,
        WorkflowCorrectness,
        AgentEvalToolChain,
        AgentEvalWorkflowNodes,
    ];

    public static IReadOnlyList<string> CasePassMetrics { get; } = [];
}
