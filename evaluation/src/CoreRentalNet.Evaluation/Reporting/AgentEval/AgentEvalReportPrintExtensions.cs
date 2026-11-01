using global::AgentEval.Exporters;
using global::AgentEval.Models;

using CoreRentalNet.Evaluation.Reporting.Html;
using CoreRentalNet.Evaluation.Reporting.Comparison;
using CoreRentalNet.Evaluation.Reporting.Writers;
namespace CoreRentalNet.Evaluation.Reporting.AgentEval;

/// <summary>Prints AgentEval's rendered summary without adding a second report format.</summary>
public static class AgentEvalReportPrintExtensions
{
    public static void Print(this EvaluationReport report)
    {
        ArgumentNullException.ThrowIfNull(report);

        Console.WriteLine(new MarkdownExporter().ExportToString(report));
    }
}
