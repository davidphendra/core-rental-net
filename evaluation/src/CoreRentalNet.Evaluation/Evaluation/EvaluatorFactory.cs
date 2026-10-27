using CoreRentalNet.Evaluation.Cases;
using CoreRentalNet.Evaluation.Evidence;
using Microsoft.Agents.AI;

namespace CoreRentalNet.Evaluation.Evaluation;

/// <summary>Builds the deterministic evaluators once, from the fixture.</summary>
public static class EvaluatorFactory
{
    public static LocalEvaluator CreateLocal(
        IReadOnlyDictionary<string, ProductRecord> catalogue)
        => new(
            EvalChecks.NonEmpty(),

            EvalChecks.ToolCalledCheck(
                ToolCalledMode.Any,
                WorkspaceToolNames.NameSearch,
                WorkspaceToolNames.MeaningSearch),

            AnswerExactCheck.Create(),
            new ConstraintComplianceCheck(catalogue).Create(),
            RetrievalQualityCheck.Create(),
            RawRetrievalQualityCheck.Create(),
            ToolCallAccuracyCheck.Create(),
            WorkflowCorrectnessCheck.Create());
}
