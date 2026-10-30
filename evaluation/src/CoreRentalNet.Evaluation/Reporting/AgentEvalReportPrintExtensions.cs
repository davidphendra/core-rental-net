using AgentEval.Exporters;
using AgentEval.Models;

namespace CoreRentalNet.Evaluation.Reporting;

/// <summary>Prints AgentEval's rendered summary without adding a second report format.</summary>
public static class AgentEvalReportPrintExtensions
{
    public static void Print(this EvaluationReport report)
    {
        ArgumentNullException.ThrowIfNull(report);

        Console.WriteLine(new MarkdownExporter().ExportToString(report));
    }
}
