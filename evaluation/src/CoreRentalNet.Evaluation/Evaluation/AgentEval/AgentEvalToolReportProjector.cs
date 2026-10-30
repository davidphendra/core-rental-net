using AgentEval.Models;
using CoreRentalNet.Evaluation.Evidence;

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
