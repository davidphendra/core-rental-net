using Azure.AI.Projects;
using CoreRentalNet.Evaluation.Cases;
using CoreRentalNet.Evaluation.Evidence;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Foundry;

namespace CoreRentalNet.Evaluation.Evaluation;

/// <summary>Builds the built-in evaluators once, from the fixture and the Foundry project.</summary>
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

    public static FoundryEvals CreateFoundry(
        AIProjectClient projectClient,
        string judgeModelDeployment)
        => new(
            projectClient,
            judgeModelDeployment,
            FoundryEvals.Similarity,
            FoundryEvals.ToolCallAccuracy,
            FoundryEvals.TaskAdherence);
}
