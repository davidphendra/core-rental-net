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

/// <summary>Builds the tool-call report AgentEval's fluent assertions read.</summary>
public static class AgentEvalToolReportProjector
{
    public static ToolUsageReport Project(EvaluationContext context)
    {
        var report = new ToolUsageReport();
        var order = 1;

        foreach (var answer in context.FinalToolAnswers)
        {
            report.AddCall(new ToolCallRecord
            {
                Name = answer.ToolName,
                CallId = $"eval-call-{order}",
                Arguments = new Dictionary<string, object?>(answer.Arguments),
                Result = answer.ResultText,
                WasExecuted = true,
                Order = order++,
            });
        }

        return report;
    }
}
