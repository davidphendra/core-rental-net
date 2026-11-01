using global::AgentEval.Models;
using CoreRentalNet.Evaluation.Evidence.Trace;
using CoreRentalNet.Evaluation.Evidence.Projection;
using CoreRentalNet.Evaluation.Evidence.Context;

using CoreRentalNet.Evaluation.Evaluation.Pipeline;
using CoreRentalNet.Evaluation.Evaluation.Checks;
using CoreRentalNet.Evaluation.Evaluation.Judging;
using CoreRentalNet.Evaluation.Evaluation.Timing;
using CoreRentalNet.Evaluation.Evaluation.Stochastic;

namespace CoreRentalNet.Evaluation.Evaluation.AgentEval;

/// <summary>Projects the observed stage facts into AgentEval's workflow result model.</summary>
public static class AgentEvalWorkflowProjector
{
    /// <summary>The workflow nodes the agent roster declares.</summary>
    public static IReadOnlyList<string> DeclaredNodes { get; } =
    [
        "verifyingRequest",
        "rephrasingRequirement",
        "retrievingCatalogueProducts",
        "rerankingWorkspaceCandidates",
        "composingWorkspaceSetups",
    ];

    public static WorkflowExecutionResult Project(EvaluationContext context, string? prompt)
    {
        var steps = context.Stages
            .Select((stage, index) => new ExecutorStep
            {
                ExecutorId = stage,
                Output = string.Empty,
                StepIndex = index,
                TimingNotMeasuredReason = "The evaluation stream records stage order, not stage duration.",
            })
            .ToArray();

        var edges = DeclaredNodes
            .Zip(DeclaredNodes.Skip(1), (from, to) => new WorkflowEdge
            {
                EdgeId = $"{from}->{to}",
                SourceExecutorId = from,
                TargetExecutorId = to,
                EdgeType = EdgeType.Sequential,
            })
            .ToArray();

        var traversed = context.Stages
            .Zip(context.Stages.Skip(1), (from, to) => new EdgeExecution
            {
                EdgeId = $"{from}->{to}",
                SourceExecutorId = from,
                TargetExecutorId = to,
                EdgeType = EdgeType.Sequential,
            })
            .ToArray();

        return new WorkflowExecutionResult
        {
            FinalOutput = EvaluationText.Candidate(context.Candidates),
            Steps = steps,
            TotalDuration = TimeSpan.Zero,
            OriginalPrompt = prompt,
            Errors = string.Equals(context.RunStatus, "success", StringComparison.Ordinal)
                ? null
                : [new WorkflowError
                  {
                      ExecutorId = "workflow",
                      Message = context.RunStatus,
                  }],
            Graph = new WorkflowGraphSnapshot
            {
                Nodes = DeclaredNodes
                    .Select((node, index) => new WorkflowNode
                    {
                        NodeId = node,
                        IsEntryPoint = index == 0,
                        IsExitNode = index == DeclaredNodes.Count - 1,
                    })
                    .ToArray(),
                Edges = edges,
                TraversedEdges = traversed,
                EntryNodeId = DeclaredNodes[0],
                ExitNodeIds = [DeclaredNodes[^1]],
            },
        };
    }
}
