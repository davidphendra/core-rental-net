using CoreRentalNet.Evaluation.Evaluation.Checks;
using CoreRentalNet.Evaluation.Evaluation.Judging;
using CoreRentalNet.Evaluation.Evaluation.Timing;
using CoreRentalNet.Evaluation.Evaluation.Stochastic;

namespace CoreRentalNet.Evaluation.Evaluation.Pipeline;

/// <summary>Stable failure codes, so a report can be grouped without parsing prose.</summary>
public static class FailureCodes
{
    public const string CaseTimeout = "CASE_TIMEOUT";
    public const string JudgeRejected = "JUDGE_REJECTED";
    public const string JudgeError = "JUDGE_ERROR";
    public const string NoCandidate = "NO_CANDIDATE";
    public const string AnswerMismatch = "ANSWER_MISMATCH";
    public const string WorkflowStatus = "WORKFLOW_STATUS";
    public const string WorkflowNoCandidate = "WORKFLOW_NO_CANDIDATE";
    public const string WorkflowCompletedNotLast = "WORKFLOW_COMPLETED_NOT_LAST";
    public const string WorkflowSequence = "WORKFLOW_SEQUENCE";

    public static string MissingSlot(string slot) => $"ANSWER_MISSING_SLOT:{slot}";
    public static string ExtraSlot(string slot) => $"ANSWER_EXTRA_SLOT:{slot}";
    public static string WrongSku(string slot) => $"ANSWER_WRONG_SKU:{slot}";
    public static string ConstraintMissingSlot(string slot) => $"CONSTRAINT_MISSING_SLOT:{slot}";
    public static string ConstraintExtraSlot(string slot) => $"CONSTRAINT_UNREQUESTED_SLOT:{slot}";
    public static string ConstraintCapacity(string slot) => $"CONSTRAINT_QUANTITY_OVER_CAPACITY:{slot}";
    public static string ConstraintDuplicateLine(string slot) => $"CONSTRAINT_DUPLICATE_LINE:{slot}";
    public static string ConstraintNegativeAmount(string slot) => $"CONSTRAINT_NEGATIVE_AMOUNT:{slot}";
    public static string ConstraintUnknownSku(string sku) => $"CONSTRAINT_UNKNOWN_SKU:{sku}";
    public static string ConstraintNotRetrieved(string sku) => $"CONSTRAINT_SKU_NOT_RETRIEVED:{sku}";
    public const string ConstraintDuplicateCandidate = "CONSTRAINT_DUPLICATE_CANDIDATE";
    public static string RetrievalMiss(string sku) => $"RETRIEVAL_MISS:{sku}";
    public static string ToolNotCalled(string slot) => $"TOOL_NOT_CALLED:{slot}";
    public static string ToolUnexpectedCall(string slot) => $"TOOL_UNEXPECTED_CALL:{slot}";
    public static string ToolInvalidArguments(string toolName) => $"TOOL_INVALID_ARGUMENTS:{toolName}";
    public static string ToolMissingArguments(string toolName) => $"TOOL_MISSING_ARGUMENTS:{toolName}";
}
