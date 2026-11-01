using CoreRentalNet.Evaluation.Cases.Model;
using CoreRentalNet.Evaluation.Cases.Parsing;
using CoreRentalNet.Evaluation.Cases.Validation;
using CoreRentalNet.Evaluation.Evidence.Trace;
using CoreRentalNet.Evaluation.Evidence.Projection;
using CoreRentalNet.Evaluation.Evidence.Context;
using CoreRentalNet.Evaluation.Evaluation.AgentEval;
using Microsoft.Agents.AI;

using CoreRentalNet.Evaluation.Evaluation.Checks;
using CoreRentalNet.Evaluation.Evaluation.Judging;
using CoreRentalNet.Evaluation.Evaluation.Timing;
using CoreRentalNet.Evaluation.Evaluation.Stochastic;
namespace CoreRentalNet.Evaluation.Evaluation.Pipeline;

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
            WorkflowCorrectnessCheck.Create(),
            AgentEvalToolChainCheck.Create(),
            AgentEvalWorkflowNodesCheck.Create());
}
